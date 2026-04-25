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
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
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
