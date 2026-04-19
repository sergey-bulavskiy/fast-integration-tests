using FastIntegrationTests.Tests.Infrastructure.IntegreSQL;
using MccSoft.IntegreSql.EF;

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
    private ShopDbContext _context = null!;

    /// <summary>Сервис для работы с товарами.</summary>
    protected IProductService ProductService { get; private set; } = null!;

    /// <summary>Сервис для работы с заказами.</summary>
    protected IOrderService OrderService { get; private set; } = null!;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        var state = await IntegresSqlContainerManager.GetStateAsync();
        _initializer = state.Initializer;
        _connectionString = await _initializer.CreateDatabaseGetConnectionString<ShopDbContext>(
            IntegresSqlDefaults.SeedingOptions);

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
        await _initializer.RemoveDatabase(_connectionString);
    }
}
