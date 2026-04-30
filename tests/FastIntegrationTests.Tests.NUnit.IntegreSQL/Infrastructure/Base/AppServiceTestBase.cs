using FastIntegrationTests.Tests.Infrastructure.IntegreSQL;
using MccSoft.IntegreSql.EF;
using Npgsql;

namespace FastIntegrationTests.Tests.Infrastructure.Base;

/// <summary>
/// Базовый класс для сервисных интеграционных тестов через IntegreSQL на NUnit.
/// На каждый тест берётся новый клон шаблонной БД (~5 мс) — изоляция полная,
/// тесты могут гоняться параллельно (см. AssemblyInfo).
/// </summary>
public abstract class AppServiceTestBase
{
    private string _connectionString = null!;
    private NpgsqlDatabaseInitializer _initializer = null!;

    /// <summary>Контекст тестовой БД. Доступен после <see cref="BaseSetUp"/>.</summary>
    protected ShopDbContext Context { get; private set; } = null!;

    /// <summary>Запускает контейнеры (при первом вызове) и клонирует шаблон.</summary>
    [SetUp]
    public async Task BaseSetUp()
    {
        var state = await IntegresSqlContainerManager.GetStateAsync();
        _initializer = state.Initializer;
        _connectionString = await _initializer.CreateDatabaseGetConnectionString<ShopDbContext>(
            IntegresSqlDefaults.SeedingOptions);
        var options = new DbContextOptionsBuilder<ShopDbContext>()
            .UseNpgsql(_connectionString).Options;
        Context = new ShopDbContext(options);
    }

    /// <summary>Освобождает контекст и возвращает клонированную БД в пул IntegreSQL.</summary>
    [TearDown]
    public async Task BaseTearDown()
    {
        await Context.DisposeAsync();
        await using var conn = new NpgsqlConnection(_connectionString);
        NpgsqlConnection.ClearPool(conn);
        await _initializer.RemoveDatabase(_connectionString);
    }
}
