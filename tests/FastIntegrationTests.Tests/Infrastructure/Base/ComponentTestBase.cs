using FastIntegrationTests.Tests.Infrastructure.IntegreSQL;
using MccSoft.IntegreSql.EF.DatabaseInitialization;

namespace FastIntegrationTests.Tests.Infrastructure.Base;

/// <summary>
/// Базовый класс для HTTP-интеграционных тестов через IntegreSQL.
/// Миграции выполняются один раз как шаблон; каждый тест получает клон (~5 мс) и отдельный TestServer.
/// Не требует [Collection] — каждый наследующий класс выполняется в своей неявной коллекции.
/// </summary>
public abstract class ComponentTestBase : IAsyncLifetime
{
    private static readonly DatabaseSeedingOptions<ShopDbContext> SeedingOptions =
        new(
            Name: "shop-default",
            SeedingFunction: async ctx => await ctx.Database.MigrateAsync(),
            DisableEnsureCreated: true,
            DbContextFactory: opts => new ShopDbContext(opts)
        );

    private string _connectionString = null!;
    private ShopDbContext _schemaContext = null!;
    private TestWebApplicationFactory _factory = null!;

    /// <summary>HTTP-клиент для обращений к тестируемому API.</summary>
    protected HttpClient Client { get; private set; } = null!;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        var state = await IntegresSqlContainerManager.GetStateAsync();
        _connectionString = await state.Initializer.CreateDatabaseGetConnectionString<ShopDbContext>(
            SeedingOptions);

        var options = new DbContextOptionsBuilder<ShopDbContext>()
            .UseNpgsql(_connectionString)
            .Options;
        _schemaContext = new ShopDbContext(options);

        _factory = new TestWebApplicationFactory("PostgreSQL", _connectionString);
        Client = _factory.CreateClient();
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        Client?.Dispose();
        if (_factory is not null) await _factory.DisposeAsync();
        await _schemaContext.DisposeAsync();
        var state = await IntegresSqlContainerManager.GetStateAsync();
        await state.Initializer.RemoveDatabase(_connectionString);
    }
}
