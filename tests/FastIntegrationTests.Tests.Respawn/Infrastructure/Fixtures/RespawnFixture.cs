using Npgsql;
using Respawn;

namespace FastIntegrationTests.Tests.Infrastructure.Fixtures;

/// <summary>
/// Получает общий контейнер PostgreSQL из <see cref="RespawnContainerManager"/>,
/// создаёт изолированную базу данных один раз на класс, применяет миграции EF Core
/// и сбрасывает данные через Respawn перед каждым тестом.
/// </summary>
public class RespawnFixture : IAsyncLifetime
{
    private string _dbName = null!;
    private string _adminConnectionString = null!;
    private Respawner _respawner = null!;

    /// <summary>Строка подключения к тестовой БД.</summary>
    public string ConnectionString { get; private set; } = null!;

    /// <inheritdoc />
    public virtual async Task InitializeAsync()
    {
        var container = await RespawnContainerManager.GetContainerAsync();
        _adminConnectionString = container.GetConnectionString();
        _dbName = $"respawn_{Guid.NewGuid():N}";

        await using var adminConn = new NpgsqlConnection(_adminConnectionString);
        await adminConn.OpenAsync();

        var migSw = System.Diagnostics.Stopwatch.StartNew();

        await using (var createCmd = adminConn.CreateCommand())
        {
            createCmd.CommandText = $"CREATE DATABASE \"{_dbName}\"";
            await createCmd.ExecuteNonQueryAsync();
        }

        var csb = new NpgsqlConnectionStringBuilder(_adminConnectionString)
        {
            Database = _dbName
        };
        ConnectionString = csb.ConnectionString;

        var options = new DbContextOptionsBuilder<ShopDbContext>()
            .UseNpgsql(ConnectionString).Options;
        await using var ctx = new ShopDbContext(options);
        await ctx.Database.MigrateAsync();

        migSw.Stop();
        BenchmarkLogger.Write("migration", migSw.ElapsedMilliseconds);

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
        BenchmarkLogger.Write("reset", sw.ElapsedMilliseconds);
    }

    /// <summary>
    /// Явный DROP DATABASE не нужен: контейнер shared и живёт ровно один процесс —
    /// Ryuk уничтожит его (и все базы в нём) сразу после завершения dotnet test.
    /// Пул Npgsql очищается явно: при большом scale накапливаются сотни уникальных
    /// connection strings (по одной на класс), каждая держит idle-соединения —
    /// без явной очистки PostgreSQL получает "too many clients".
    /// </summary>
    public virtual Task DisposeAsync()
    {
        if (ConnectionString is not null)
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            NpgsqlConnection.ClearPool(conn);
        }
        return Task.CompletedTask;
    }
}
