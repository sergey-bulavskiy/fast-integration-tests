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

        var pgContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithNetwork(network)
            .WithNetworkAliases("postgres")
            .WithCommand("-c", "max_connections=500")
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
            // Без этого RemoveDatabase — no-op, и IntegreSQL не знает, что тест завершился.
            // Он принудительно рециклирует слоты (DROP + CREATE FROM TEMPLATE) по истечении
            // пула (24 БД), попадая на долгие тесты вроде FullLifecycle → 3D000 под нагрузкой.
            // При DropDatabaseOnRemove=true каждый DisposeAsync вызывает POST .../recreate:
            // IntegreSQL сразу помечает БД как свободную и пересоздаёт её чисто.
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
        Console.WriteLine($"##BENCH[migration]={migSw.ElapsedMilliseconds}");
        await initializer.RemoveDatabase(warmupCs);

        return new IntegresSqlState(initializer);
    }
}
