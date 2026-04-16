using FastIntegrationTests.Application.Interfaces;
using FastIntegrationTests.Application.Services;
using FastIntegrationTests.Infrastructure.Data;
using FastIntegrationTests.Infrastructure.Repositories;
using FastIntegrationTests.Tests.Infrastructure.Factories;
using FastIntegrationTests.Tests.Infrastructure.Fixtures;

namespace FastIntegrationTests.Tests.Infrastructure.Base;

/// <summary>
/// Базовый класс для интеграционных тестов сервисного уровня.
/// Создаёт изолированную БД на каждый тест, предоставляет готовые сервисы.
/// </summary>
public abstract class ServiceTestBase : IAsyncLifetime
{
    private readonly ContainerFixture _fixture;
    private ShopDbContext _context = null!;

    /// <summary>Сервис для работы с товарами.</summary>
    protected IProductService ProductService { get; private set; } = null!;

    /// <summary>Сервис для работы с заказами.</summary>
    protected IOrderService OrderService { get; private set; } = null!;

    /// <param name="fixture">Запущенный контейнер с СУБД.</param>
    protected ServiceTestBase(ContainerFixture fixture) => _fixture = fixture;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        var factory = new TestDbFactory(_fixture);
        _context = await factory.CreateAsync();

        var productRepo = new ProductRepository(_context);
        var orderRepo = new OrderRepository(_context);

        ProductService = new ProductService(productRepo);
        OrderService = new OrderService(orderRepo, productRepo);
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }
}
