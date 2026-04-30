using FastIntegrationTests.Tests.Infrastructure.IntegreSQL;
using MccSoft.IntegreSql.EF;
using Npgsql;

namespace FastIntegrationTests.Tests.Infrastructure.Base;

/// <summary>
/// Базовый класс для HTTP-интеграционных тестов через IntegreSQL на NUnit.
/// На каждый тест — свой клон шаблонной БД (~5 мс) и отдельный TestServer.
/// </summary>
public abstract class ComponentTestBase
{
    private string _connectionString = null!;
    private NpgsqlDatabaseInitializer _initializer = null!;
    private TestWebApplicationFactory _factory = null!;

    /// <summary>HTTP-клиент для обращений к тестируемому API.</summary>
    protected HttpClient Client { get; private set; } = null!;

    /// <summary>Запускает контейнеры (при первом вызове), клонирует шаблон, поднимает TestServer.</summary>
    [SetUp]
    public async Task BaseSetUp()
    {
        var state = await IntegresSqlContainerManager.GetStateAsync();
        _initializer = state.Initializer;
        _connectionString = await _initializer.CreateDatabaseGetConnectionString<ShopDbContext>(
            IntegresSqlDefaults.SeedingOptions);

        _factory = new TestWebApplicationFactory(_connectionString);
        Client = _factory.CreateClient();
    }

    /// <summary>Диспозит клиент и фабрику, возвращает БД в пул.</summary>
    [TearDown]
    public async Task BaseTearDown()
    {
        Client?.Dispose();
        if (_factory is not null)
            // PhysicalFilesWatcher внутри WebApplicationFactory может бросить NullReferenceException
            // при диспозе под высокой параллельностью — баг в ASP.NET Core FileSystemWatcher.
            try { await _factory.DisposeAsync(); } catch (NullReferenceException) { }
        await using var conn = new NpgsqlConnection(_connectionString);
        NpgsqlConnection.ClearPool(conn);
        await _initializer.RemoveDatabase(_connectionString);
    }
}
