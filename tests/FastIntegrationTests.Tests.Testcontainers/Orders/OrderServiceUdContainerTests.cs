namespace FastIntegrationTests.Tests.Testcontainers.Orders;

/// <summary>
/// Тесты сервисного уровня: статусные переходы для OrderService.
/// Каждый тест создаёт изолированную БД в реальном контейнере PostgreSQL.
/// </summary>
public class OrderServiceUdContainerTests : ContainerServiceTestBase
{
    private IOrderService Sut => OrderService;
    private IProductService _products => ProductService;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="OrderServiceUdContainerTests"/>.
    /// </summary>
    /// <param name="fixture">Запущенный контейнер с СУБД.</param>
    public OrderServiceUdContainerTests(ContainerFixture fixture) : base(fixture) { }

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

        var fetched = await Sut.GetByIdAsync(order.Id);
        Assert.Equal(OrderStatus.Completed, fetched.Status);
    }

    /// <summary>
    /// Создаёт заказ с тремя позициями разных товаров — проверяет итоговую сумму и состав.
    /// </summary>
    [Fact]
    public async Task MultiItemOrder_TotalAmountAndItemsCorrect()
    {
        var p1 = await _products.CreateAsync(new CreateProductRequest { Name = "Телефон", Price = 30_000m });
        var p2 = await _products.CreateAsync(new CreateProductRequest { Name = "Чехол", Price = 500m });
        var p3 = await _products.CreateAsync(new CreateProductRequest { Name = "Зарядка", Price = 1_500m });

        var order = await Sut.CreateAsync(new CreateOrderRequest
        {
            Items = new List<OrderItemRequest>
            {
                new() { ProductId = p1.Id, Quantity = 1 },  // 30_000
                new() { ProductId = p2.Id, Quantity = 2 },  // 1_000
                new() { ProductId = p3.Id, Quantity = 1 },  // 1_500
            }
        });

        Assert.Equal(32_500m, order.TotalAmount);
        Assert.Equal(3, order.Items.Count);

        var fetched = await Sut.GetByIdAsync(order.Id);
        Assert.Equal(32_500m, fetched.TotalAmount);
        Assert.Equal(3, fetched.Items.Count);

        // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
        var extraProduct = await _products.CreateAsync(new CreateProductRequest { Name = "Доп товар", Price = 100m });
        for (var i = 0; i < 3; i++)
        {
            var extra = await Sut.CreateAsync(new CreateOrderRequest
            {
                Items = new List<OrderItemRequest> { new() { ProductId = extraProduct.Id, Quantity = 1 } }
            });
            await Sut.GetByIdAsync(extra.Id);
        }
        await Sut.GetAllAsync();
    }

    /// <summary>
    /// Создаёт заказ с тремя позициями, проводит полный lifecycle, проверяет итоговую сумму и статус.
    /// </summary>
    [Fact]
    public async Task MultiItemLifecycle_FullPath_TotalAmountAndStatusCorrect()
    {
        var p1 = await _products.CreateAsync(new CreateProductRequest { Name = "Ноутбук", Price = 50_000m });
        var p2 = await _products.CreateAsync(new CreateProductRequest { Name = "Мышь", Price = 2_000m });
        var p3 = await _products.CreateAsync(new CreateProductRequest { Name = "Клавиатура", Price = 3_000m });

        var order = await Sut.CreateAsync(new CreateOrderRequest
        {
            Items = new List<OrderItemRequest>
            {
                new() { ProductId = p1.Id, Quantity = 1 },  // 50_000
                new() { ProductId = p2.Id, Quantity = 1 },  // 2_000
                new() { ProductId = p3.Id, Quantity = 2 },  // 6_000
            }
        });

        // 50_000 + 2_000 + 3_000 * 2 = 58_000
        Assert.Equal(58_000m, order.TotalAmount);
        Assert.Equal(OrderStatus.New, order.Status);

        await Sut.ConfirmAsync(order.Id);
        await Sut.ShipAsync(order.Id);
        var completed = await Sut.CompleteAsync(order.Id);
        Assert.Equal(OrderStatus.Completed, completed.Status);

        var fetched = await Sut.GetByIdAsync(order.Id);
        Assert.Equal(OrderStatus.Completed, fetched.Status);
        Assert.Equal(58_000m, fetched.TotalAmount);
        Assert.Equal(3, fetched.Items.Count);
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
