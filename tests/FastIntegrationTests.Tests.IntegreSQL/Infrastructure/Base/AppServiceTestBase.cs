using FastIntegrationTests.Tests.Infrastructure.IntegreSQL;
using MccSoft.IntegreSql.EF;
using Npgsql;

namespace FastIntegrationTests.Tests.Infrastructure.Base;

/// <summary>
/// Базовый класс для сервисных интеграционных тестов через IntegreSQL.
/// Миграции выполняются один раз как шаблон; каждый тест получает клон (~5 мс).
/// Не требует [Collection] — каждый наследующий класс выполняется в своей неявной коллекции.
/// </summary>
public abstract class AppServiceTestBase : IAsyncLifetime
{
    private string _connectionString = null!;
    private NpgsqlDatabaseInitializer _initializer = null!;

    /// <summary>Контекст тестовой БД. Доступен после InitializeAsync.</summary>
    protected ShopDbContext Context { get; private set; } = null!;

    /// <summary>Инициализирует тестовую БД: запускает контейнеры (при первом вызове) и клонирует шаблон.</summary>
    public virtual async Task InitializeAsync()
    {
        var state = await IntegresSqlContainerManager.GetStateAsync();
        _initializer = state.Initializer;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        _connectionString = await _initializer.CreateDatabaseGetConnectionString<ShopDbContext>(
            IntegresSqlDefaults.SeedingOptions);
        sw.Stop();
        BenchmarkLogger.Write("clone", sw.ElapsedMilliseconds);
        var options = new DbContextOptionsBuilder<ShopDbContext>()
            .UseNpgsql(_connectionString).Options;
        Context = new ShopDbContext(options);
    }

    /// <summary>Освобождает контекст и возвращает клонированную БД в пул IntegreSQL с пометкой «пересоздать из шаблона».</summary>
    public virtual async Task DisposeAsync()
    {
        await Context.DisposeAsync();
        await using var conn = new NpgsqlConnection(_connectionString);
        NpgsqlConnection.ClearPool(conn);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await _initializer.RemoveDatabase(_connectionString);
        sw.Stop();
        BenchmarkLogger.Write("reset", sw.ElapsedMilliseconds);
    }
}
