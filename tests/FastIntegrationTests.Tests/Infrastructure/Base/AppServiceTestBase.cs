using FastIntegrationTests.Tests.Infrastructure.IntegreSQL;
using MccSoft.IntegreSql.EF.DatabaseInitialization;

namespace FastIntegrationTests.Tests.Infrastructure.Base;

/// <summary>
/// Базовый класс для сервисных интеграционных тестов через IntegreSQL.
/// Миграции выполняются один раз как шаблон; каждый тест получает клон (~5 мс).
/// Не требует [Collection] — каждый наследующий класс выполняется в своей неявной коллекции.
/// </summary>
public abstract class AppServiceTestBase : IAsyncLifetime
{
    private static readonly DatabaseSeedingOptions<ShopDbContext> SeedingOptions =
        new(
            Name: "shop-default",
            SeedingFunction: async ctx => await ctx.Database.MigrateAsync(),
            DisableEnsureCreated: true,
            DbContextFactory: opts => new ShopDbContext(opts)
        );

    private string _connectionString = null!;
    private ShopDbContext _context = null!;

    /// <summary>Сервис для работы с товарами.</summary>
    protected IProductService ProductService { get; private set; } = null!;

    /// <summary>Сервис для работы с заказами.</summary>
    protected IOrderService OrderService { get; private set; } = null!;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        var state = await IntegresSqlContainerManager.GetStateAsync();
        _connectionString = await state.Initializer.CreateDatabaseGetConnectionString<ShopDbContext>(
            SeedingOptions);

        var options = new DbContextOptionsBuilder<ShopDbContext>()
            .UseNpgsql(_connectionString)
            .Options;
        _context = new ShopDbContext(options);

        var productRepo = new ProductRepository(_context);
        var orderRepo = new OrderRepository(_context);
        ProductService = new ProductService(productRepo);
        OrderService = new OrderService(orderRepo, productRepo);
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        var state = await IntegresSqlContainerManager.GetStateAsync();
        await state.Initializer.RemoveDatabase(_connectionString);
    }
}
