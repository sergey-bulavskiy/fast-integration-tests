namespace FastIntegrationTests.Tests.Testcontainers.Orders;

/// <summary>
/// Интеграционные тесты сервисного уровня для OrderService через Testcontainers.
/// Используются как baseline для сравнения скорости с IntegreSQL-вариантом (<see cref="OrderServiceTests"/>).
/// Проверяют CRUD, расчёт суммы, фиксацию цены и все переходы статусов.
/// Каждый тест создаёт изолированную БД в реальном контейнере PostgreSQL.
/// </summary>
[Collection("OrdersServiceContainer")]
public class OrderServiceContainerTests : ServiceTestBase
{
    private IOrderService Sut => OrderService;
    private IProductService _products => ProductService;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="OrderServiceContainerTests"/>.
    /// </summary>
    /// <param name="fixture">Запущенный контейнер с СУБД.</param>
    public OrderServiceContainerTests(ContainerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task GetAllAsync_WhenNoOrders_ReturnsEmptyList()
    {
        var result = await Sut.GetAllAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_WhenOrdersExist_ReturnsAllOrders()
    {
        var product = await _products.CreateAsync(new CreateProductRequest { Name = "Товар", Price = 100m });
        await Sut.CreateAsync(new CreateOrderRequest
        {
            Items = new List<OrderItemRequest> { new() { ProductId = product.Id, Quantity = 1 } }
        });
        await Sut.CreateAsync(new CreateOrderRequest
        {
            Items = new List<OrderItemRequest> { new() { ProductId = product.Id, Quantity = 2 } }
        });

        var result = await Sut.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetByIdAsync_WhenOrderExists_ReturnsOrderWithItems()
    {
        var product = await _products.CreateAsync(new CreateProductRequest { Name = "Товар", Price = 500m });
        var created = await Sut.CreateAsync(new CreateOrderRequest
        {
            Items = new List<OrderItemRequest> { new() { ProductId = product.Id, Quantity = 3 } }
        });

        var result = await Sut.GetByIdAsync(created.Id);

        Assert.Equal(created.Id, result.Id);
        Assert.Single(result.Items);
        Assert.Equal(product.Id, result.Items[0].ProductId);
        Assert.Equal(3, result.Items[0].Quantity);
    }

    [Fact]
    public async Task GetByIdAsync_WhenOrderNotFound_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Sut.GetByIdAsync(999));
    }

    [Fact]
    public async Task CreateAsync_CalculatesTotalAmountCorrectly()
    {
        var product1 = await _products.CreateAsync(new CreateProductRequest { Name = "Товар 1", Price = 100m });
        var product2 = await _products.CreateAsync(new CreateProductRequest { Name = "Товар 2", Price = 200m });

        var order = await Sut.CreateAsync(new CreateOrderRequest
        {
            Items = new List<OrderItemRequest>
            {
                new() { ProductId = product1.Id, Quantity = 2 }, // 2 * 100 = 200
                new() { ProductId = product2.Id, Quantity = 3 }, // 3 * 200 = 600
            }
        });

        Assert.Equal(800m, order.TotalAmount); // 200 + 600
    }

    [Fact]
    public async Task CreateAsync_SetsUnitPriceFromCurrentProductPrice()
    {
        var product = await _products.CreateAsync(new CreateProductRequest { Name = "Товар", Price = 999m });

        var order = await Sut.CreateAsync(new CreateOrderRequest
        {
            Items = new List<OrderItemRequest> { new() { ProductId = product.Id, Quantity = 1 } }
        });

        Assert.Equal(999m, order.Items[0].UnitPrice);
    }

    [Fact]
    public async Task CreateAsync_WhenProductNotFound_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Sut.CreateAsync(new CreateOrderRequest
        {
            Items = new List<OrderItemRequest> { new() { ProductId = 999, Quantity = 1 } }
        }));
    }

    [Fact]
    public async Task CreateAsync_NewOrderHasStatusNew()
    {
        var product = await _products.CreateAsync(new CreateProductRequest { Name = "Товар", Price = 100m });

        var order = await Sut.CreateAsync(new CreateOrderRequest
        {
            Items = new List<OrderItemRequest> { new() { ProductId = product.Id, Quantity = 1 } }
        });

        Assert.Equal(OrderStatus.New, order.Status);
    }

    [Fact]
    public async Task ConfirmAsync_ChangesStatusFromNewToConfirmed()
    {
        var order = await CreateOrderAsync();

        var confirmed = await Sut.ConfirmAsync(order.Id);

        Assert.Equal(OrderStatus.Confirmed, confirmed.Status);
    }

    [Fact]
    public async Task ShipAsync_ChangesStatusFromConfirmedToShipped()
    {
        var order = await CreateOrderAsync();
        await Sut.ConfirmAsync(order.Id);

        var shipped = await Sut.ShipAsync(order.Id);

        Assert.Equal(OrderStatus.Shipped, shipped.Status);
    }

    [Fact]
    public async Task CompleteAsync_ChangesStatusFromShippedToCompleted()
    {
        var order = await CreateOrderAsync();
        await Sut.ConfirmAsync(order.Id);
        await Sut.ShipAsync(order.Id);

        var completed = await Sut.CompleteAsync(order.Id);

        Assert.Equal(OrderStatus.Completed, completed.Status);
    }

    [Fact]
    public async Task CancelAsync_ChangesStatusFromNewToCancelled()
    {
        var order = await CreateOrderAsync();

        var cancelled = await Sut.CancelAsync(order.Id);

        Assert.Equal(OrderStatus.Cancelled, cancelled.Status);
    }

    [Fact]
    public async Task CancelAsync_ChangesStatusFromConfirmedToCancelled()
    {
        var order = await CreateOrderAsync();
        await Sut.ConfirmAsync(order.Id);

        var cancelled = await Sut.CancelAsync(order.Id);

        Assert.Equal(OrderStatus.Cancelled, cancelled.Status);
    }

    [Fact]
    public async Task ConfirmAsync_WhenOrderIsCompleted_ThrowsInvalidOrderStatusTransitionException()
    {
        var order = await CreateOrderAsync();
        await Sut.ConfirmAsync(order.Id);
        await Sut.ShipAsync(order.Id);
        await Sut.CompleteAsync(order.Id);

        await Assert.ThrowsAsync<InvalidOrderStatusTransitionException>(
            () => Sut.ConfirmAsync(order.Id));
    }

    [Fact]
    public async Task CancelAsync_WhenOrderIsShipped_ThrowsInvalidOrderStatusTransitionException()
    {
        var order = await CreateOrderAsync();
        await Sut.ConfirmAsync(order.Id);
        await Sut.ShipAsync(order.Id);

        await Assert.ThrowsAsync<InvalidOrderStatusTransitionException>(
            () => Sut.CancelAsync(order.Id));
    }

    [Fact]
    public async Task FullLifecycle_CreateConfirmShipComplete_StatusCorrectAtEachStep()
    {
        var order = await CreateOrderAsync();
        Assert.Equal(OrderStatus.New, order.Status);

        var confirmed = await Sut.ConfirmAsync(order.Id);
        Assert.Equal(OrderStatus.Confirmed, confirmed.Status);

        var shipped = await Sut.ShipAsync(order.Id);
        Assert.Equal(OrderStatus.Shipped, shipped.Status);

        var completed = await Sut.CompleteAsync(order.Id);
        Assert.Equal(OrderStatus.Completed, completed.Status);

        // Проверяем финальный статус через повторный запрос
        var fetched = await Sut.GetByIdAsync(order.Id);
        Assert.Equal(OrderStatus.Completed, fetched.Status);
    }

    // --- helpers ---

    /// <summary>
    /// Создаёт товар и заказ с одной позицией, возвращает DTO заказа.
    /// </summary>
    /// <param name="ct">Токен отмены операции.</param>
    private async Task<OrderDto> CreateOrderAsync(CancellationToken ct = default)
    {
        var product = await _products.CreateAsync(new CreateProductRequest { Name = "Товар", Price = 100m }, ct);
        return await Sut.CreateAsync(new CreateOrderRequest
        {
            Items = new List<OrderItemRequest> { new() { ProductId = product.Id, Quantity = 1 } }
        }, ct);
    }
}
