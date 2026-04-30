using System.Diagnostics;
using Npgsql;

namespace FastIntegrationTests.Tests.Infrastructure;

/// <summary>
/// Внутренний хелпер для жизненного цикла одной тестовой БД на общем контейнере:
/// создание + миграция при старте теста, очистка пула + DROP DATABASE при завершении.
/// </summary>
/// <remarks>
/// Используется обоими базовыми классами <see cref="Base.SharedServiceTestBase"/>
/// и <see cref="Base.SharedApiTestBase"/>, чтобы не дублировать ~25 строк lifecycle.
/// </remarks>
internal sealed class SharedDbHandle
{
    /// <summary>Строка подключения к созданной БД. Доступна после <see cref="CreateAndMigrateAsync"/>.</summary>
    public string ConnectionString { get; private set; } = null!;

    private string _dbName = null!;

    /// <summary>
    /// Создаёт уникальную БД <c>test_{guid}</c> на общем контейнере и применяет миграции EF Core.
    /// При сбое миграции делает best-effort <c>DROP DATABASE</c> и перебрасывает исключение —
    /// xUnit при провале <c>InitializeAsync</c> не вызывает <c>DisposeAsync</c>.
    /// </summary>
    public async Task CreateAndMigrateAsync()
    {
        var container = await SharedContainerManager.GetContainerAsync();
        _dbName = $"test_{Guid.NewGuid():N}";
        var adminCs = container.GetConnectionString();

        await using (var admin = new NpgsqlConnection(adminCs))
        {
            await admin.OpenAsync();
            await using var cmd = admin.CreateCommand();
            cmd.CommandText = $"CREATE DATABASE \"{_dbName}\"";
            await cmd.ExecuteNonQueryAsync();
        }

        ConnectionString = new NpgsqlConnectionStringBuilder(adminCs) { Database = _dbName }
            .ConnectionString;

        var options = new DbContextOptionsBuilder<ShopDbContext>()
            .UseNpgsql(ConnectionString).Options;
        await using var ctx = new ShopDbContext(options);

        var sw = Stopwatch.StartNew();
        try
        {
            await ctx.Database.MigrateAsync();
        }
        catch
        {
            // БД создана, но миграция упала — нужно прибрать, иначе она будет висеть в контейнере
            // до завершения процесса (DisposeAsync xUnit не вызовет).
            try { await DropAsync(); } catch { /* swallow — cleanup best-effort */ }
            throw;
        }
        sw.Stop();
        BenchmarkLogger.Write("migration", sw.ElapsedMilliseconds);
    }

    /// <summary>
    /// Освобождает пул соединений Npgsql и дропает БД через admin-соединение.
    /// </summary>
    public async Task DropAsync()
    {
        var sw = Stopwatch.StartNew();

        // Без ClearPool DROP DATABASE упадёт с "database is being accessed by other users"
        // из-за idle-соединений, висящих в пуле Npgsql.
        await using (var conn = new NpgsqlConnection(ConnectionString))
            NpgsqlConnection.ClearPool(conn);

        var container = await SharedContainerManager.GetContainerAsync();
        await using var admin = new NpgsqlConnection(container.GetConnectionString());
        await admin.OpenAsync();
        await using var cmd = admin.CreateCommand();
        cmd.CommandText = $"DROP DATABASE IF EXISTS \"{_dbName}\"";
        await cmd.ExecuteNonQueryAsync();

        sw.Stop();
        BenchmarkLogger.Write("reset", sw.ElapsedMilliseconds);
    }
}
