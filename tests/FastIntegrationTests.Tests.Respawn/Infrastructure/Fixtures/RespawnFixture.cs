using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;

namespace FastIntegrationTests.Tests.Infrastructure.Fixtures;

/// <summary>
/// Запускает контейнер PostgreSQL, применяет миграции один раз на класс
/// и сбрасывает данные через Respawn перед каждым тестом.
/// </summary>
public class RespawnFixture : IAsyncLifetime
{
    private PostgreSqlContainer _container = null!;
    private Respawner _respawner = null!;

    /// <summary>Строка подключения к тестовой БД.</summary>
    public string ConnectionString { get; private set; } = null!;

    /// <inheritdoc />
    public virtual async Task InitializeAsync()
    {
        // Параметры производительности PostgreSQL для тестовой среды.
        // Рекомендованы авторами IntegreSQL в официальном docker-compose.yml:
        // https://github.com/allaboutapps/integresql/blob/master/README.md
        // Совокупно дают ~30% ускорение за счёт отключения гарантий долговечности WAL,
        // которые необходимы в продакшне, но бессмысленны для эфемерных тестовых данных.
        // ⚠ НИКОГДА не переносить в продакшн — при сбое питания/краше возможна потеря данных.
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithCommand(
                // fsync=off: PostgreSQL не вызывает fsync() для сброса WAL на диск.
                // В продакшне защищает от потери коммитов при сбое питания.
                // В тесте контейнер эфемерный — защита не нужна, а ожидание IO — главный тормоз.
                // Docs: https://www.postgresql.org/docs/current/runtime-config-wal.html#GUC-FSYNC
                "-c", "fsync=off",
                // synchronous_commit=off: сервер подтверждает транзакцию клиенту не дожидаясь
                // записи WAL на диск. С fsync=off основной эффект уже достигнут, но явное
                // отключение дополнительно убирает задержки синхронизации со standby-репликами.
                // Docs: https://www.postgresql.org/docs/current/runtime-config-wal.html#GUC-SYNCHRONOUS-COMMIT
                "-c", "synchronous_commit=off",
                // full_page_writes=off: PostgreSQL не записывает полную страницу в WAL после
                // чекпоинта. С fsync=off частичная запись страниц невозможна, поэтому флаг
                // избыточен. Отключение снижает объём WAL-записей.
                // Docs: https://www.postgresql.org/docs/current/runtime-config-wal.html#GUC-FULL-PAGE-WRITES
                "-c", "full_page_writes=off",
                // shared_buffers=128MB: размер общего буферного кеша. Дефолт в alpine-образе
                // — 32MB. 128MB снижает количество дисковых чтений при повторных обращениях
                // к одним страницам между тестами.
                // Docs: https://www.postgresql.org/docs/current/runtime-config-resource.html#GUC-SHARED-BUFFERS
                "-c", "shared_buffers=128MB"
            )
            .Build();
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();

        var options = new DbContextOptionsBuilder<ShopDbContext>()
            .UseNpgsql(ConnectionString).Options;
        await using var ctx = new ShopDbContext(options);

        var migSw = System.Diagnostics.Stopwatch.StartNew();
        await ctx.Database.MigrateAsync();
        migSw.Stop();
        Console.WriteLine($"##BENCH[migration]={migSw.ElapsedMilliseconds}");

        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        _respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
        });
    }

    /// <summary>Сбрасывает все данные через Respawn (схема сохраняется).</summary>
    public async Task ResetAsync()
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await _respawner.ResetAsync(conn);
        sw.Stop();
        Console.WriteLine($"##BENCH[reset]={sw.ElapsedMilliseconds}");
    }

    /// <inheritdoc />
    public virtual async Task DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }
}
