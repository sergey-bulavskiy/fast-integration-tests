using DotNet.Testcontainers.Builders;
using MccSoft.IntegreSql.EF;
using MccSoft.IntegreSql.EF.DatabaseInitialization;
using Testcontainers.PostgreSql;

namespace FastIntegrationTests.Tests.Infrastructure.IntegreSQL;

/// <summary>
/// Запускает PostgreSQL и IntegreSQL-сервер в Docker-контейнерах один раз на весь процесс.
/// Все тест-классы, использующие IntegreSQL, разделяют одну пару контейнеров.
/// </summary>
/// <remarks>
/// Контейнеры не останавливаются при завершении процесса — это намеренно для учебного проекта.
/// Testcontainers использует Ryuk-агент для автоматической очистки осиротевших контейнеров.
/// </remarks>
public static class IntegresSqlContainerManager
{
    private static readonly Lazy<Task<IntegresSqlState>> _state =
        new(() => InitializeAsync(), LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// Возвращает готовое состояние IntegreSQL.
    /// При первом вызове запускает контейнеры — последующие вызовы возвращают кешированный результат.
    /// </summary>
    public static Task<IntegresSqlState> GetStateAsync() => _state.Value;

    private static async Task<IntegresSqlState> InitializeAsync()
    {
        var network = new NetworkBuilder().Build();
        await network.CreateAsync();

        // Параметры производительности PostgreSQL для тестовой среды.
        // Рекомендованы авторами IntegreSQL в официальном docker-compose.yml:
        // https://github.com/allaboutapps/integresql/blob/master/README.md
        // Совокупно дают ~30% ускорение за счёт отключения гарантий долговечности WAL,
        // которые необходимы в продакшне, но бессмысленны для эфемерных тестовых данных.
        // ⚠ НИКОГДА не переносить в продакшн — при сбое питания/краше возможна потеря данных.
        var pgContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithNetwork(network)
            .WithNetworkAliases("postgres")
            .WithCommand(
                // IntegreSQL открывает соединение на каждый клон шаблона; при параллельных
                // тестах дефолтных 100 не хватает.
                "-c", "max_connections=500",
                // fsync=off: PostgreSQL не вызывает fsync() для сброса WAL на диск перед
                // подтверждением транзакции. В продакшне защищает от потери коммитов при сбое
                // питания. В тесте контейнер эфемерный — защита не нужна, а ожидание IO
                // является главным источником задержки.
                // Docs: https://www.postgresql.org/docs/current/runtime-config-wal.html#GUC-FSYNC
                "-c", "fsync=off",
                // synchronous_commit=off: сервер подтверждает транзакцию клиенту не дожидаясь
                // записи WAL на диск. С fsync=off основной эффект уже достигнут, но явное
                // отключение дополнительно убирает любые задержки синхронизации включая
                // ожидание standby-реплик.
                // Docs: https://www.postgresql.org/docs/current/runtime-config-wal.html#GUC-SYNCHRONOUS-COMMIT
                "-c", "synchronous_commit=off",
                // full_page_writes=off: при включённом режиме PostgreSQL записывает полную
                // страницу в WAL после каждого чекпоинта — для восстановления после частичной
                // записи при сбое диска. С fsync=off частичная запись страниц невозможна,
                // поэтому флаг избыточен. Отключение снижает объём WAL-записей.
                // Docs: https://www.postgresql.org/docs/current/runtime-config-wal.html#GUC-FULL-PAGE-WRITES
                "-c", "full_page_writes=off",
                // shared_buffers=128MB: размер общего буферного кеша. Дефолт в alpine-образе
                // — 32MB, что мало для частых операций с шаблонными БД. 128MB снижает
                // количество дисковых чтений при повторном доступе к одним страницам.
                // Docs: https://www.postgresql.org/docs/current/runtime-config-resource.html#GUC-SHARED-BUFFERS
                "-c", "shared_buffers=128MB"
            )
            .Build();
        await pgContainer.StartAsync();

        var integreSqlContainer = new ContainerBuilder()
            .WithImage("ghcr.io/allaboutapps/integresql:latest")
            .WithNetwork(network)
            .WithEnvironment("PGHOST", "postgres")
            .WithEnvironment("PGUSER", "postgres")
            .WithEnvironment("PGPASSWORD", "postgres")
            .WithEnvironment("PGPORT", "5432")
            .WithPortBinding(5000, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("http server started"))
            .Build();
        await integreSqlContainer.StartAsync();

        var initializer = new NpgsqlDatabaseInitializer(
            integreSqlUri: new Uri(
                $"http://localhost:{integreSqlContainer.GetMappedPublicPort(5000)}/api/v1/"),
            connectionStringOverride: new ConnectionStringOverride
            {
                Host = "localhost",
                Port = pgContainer.GetMappedPublicPort(5432)
            }
        )
        {
            // Без этого RemoveDatabase — no-op: IntegreSQL не получает явного сигнала и полагается
            // только на авто-FIFO пересоздание. При высоком параллелизме и долгих тестах FIFO
            // не успевает — пул (runtime.NumCPU() * 4 слотов) переполняется, IntegreSQL
            // принудительно рециклирует занятые слоты → долгой тест теряет БД → 3D000.
            // При DropDatabaseOnRemove=true каждый DisposeAsync вызывает POST .../recreate:
            // IntegreSQL немедленно освобождает слот и ставит пересоздание в очередь чисто.
            DropDatabaseOnRemove = true,
        };

        // Прогреваем шаблонную БД до того, как параллельные тест-классы начнут за неё гонку.
        // Lazy<Task<>> гарантирует, что этот код выполнится ровно один раз до того, как
        // GetStateAsync() вернёт результат. Без прогрева все N параллельных классов одновременно
        // видят «шаблон не готов» и пытаются его создать — MccSoft.IntegreSql.EF пересоздаёт
        // шаблон на каждую попытку, что дропает уже выданные клоны.
        // RemoveDatabase здесь правомерен: DropDatabaseOnRemove=true вызывает POST .../recreate,
        // который возвращает прогревочную БД в пул чисто, без гонок.
        var migSw = System.Diagnostics.Stopwatch.StartNew();
        var warmupCs = await initializer.CreateDatabaseGetConnectionString(
            IntegresSqlDefaults.SeedingOptions);
        migSw.Stop();
        BenchmarkLogger.Write("migration", migSw.ElapsedMilliseconds);
        await initializer.RemoveDatabase(warmupCs);

        return new IntegresSqlState(initializer);
    }
}
