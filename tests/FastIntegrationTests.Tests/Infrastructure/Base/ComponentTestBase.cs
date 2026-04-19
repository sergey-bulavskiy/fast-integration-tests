using FastIntegrationTests.Tests.Infrastructure.IntegreSQL;
using MccSoft.IntegreSql.EF;

namespace FastIntegrationTests.Tests.Infrastructure.Base;

/// <summary>
/// Базовый класс для HTTP-интеграционных тестов через IntegreSQL.
/// Миграции выполняются один раз как шаблон; каждый тест получает клон (~5 мс) и отдельный TestServer.
/// Не требует [Collection] — каждый наследующий класс выполняется в своей неявной коллекции.
/// </summary>
public abstract class ComponentTestBase : IAsyncLifetime
{
    private string _connectionString = null!;
    private NpgsqlDatabaseInitializer _initializer = null!;
    private TestWebApplicationFactory _factory = null!;

    /// <summary>HTTP-клиент для обращений к тестируемому API.</summary>
    protected HttpClient Client { get; private set; } = null!;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        var state = await IntegresSqlContainerManager.GetStateAsync();
        _initializer = state.Initializer;
        _connectionString = await _initializer.CreateDatabaseGetConnectionString<ShopDbContext>(
            IntegresSqlDefaults.SeedingOptions);

        _factory = new TestWebApplicationFactory("PostgreSQL", _connectionString);
        Client = _factory.CreateClient();
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        Client?.Dispose();
        if (_factory is not null) await _factory.DisposeAsync();
        await _initializer.RemoveDatabase(_connectionString);
    }
}
