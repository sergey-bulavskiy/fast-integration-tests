namespace FastIntegrationTests.Tests.Respawn.Orders;

/// <summary>
/// Тесты сервисного уровня: статусные переходы для OrderService.
/// Каждый тест сбрасывает данные через Respawn (~1 мс), схема сохраняется.
/// </summary>
public class OrderServiceUdRespawnTests : RespawnServiceTestBase
{
    /// <summary>Создаёт новый экземпляр <see cref="OrderServiceUdRespawnTests"/>.</summary>
    /// <param name="fixture">Фикстура с контейнером и Respawner.</param>
    public OrderServiceUdRespawnTests(RespawnFixture fixture) : base(fixture) { }

    private IOrderService Sut = null!;
    private IProductService _products = null!;

    /// <inheritdoc/>
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        var productRepo = new ProductRepository(Context);
        _products = new ProductService(productRepo);
        Sut = new OrderService(new OrderRepository(Context), productRepo);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task ConfirmAsync_ChangesStatusFromNewToConfirmed(int _)
    {
        var order = await CreateOrderAsync();

        var confirmed = await Sut.ConfirmAsync(order.Id);

        Assert.Equal(OrderStatus.Confirmed, confirmed.Status);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task ShipAsync_ChangesStatusFromConfirmedToShipped(int _)
    {
        var order = await CreateOrderAsync();
        await Sut.ConfirmAsync(order.Id);

        var shipped = await Sut.ShipAsync(order.Id);

        Assert.Equal(OrderStatus.Shipped, shipped.Status);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task CompleteAsync_ChangesStatusFromShippedToCompleted(int _)
    {
        var order = await CreateOrderAsync();
        await Sut.ConfirmAsync(order.Id);
        await Sut.ShipAsync(order.Id);

        var completed = await Sut.CompleteAsync(order.Id);

        Assert.Equal(OrderStatus.Completed, completed.Status);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task CancelAsync_ChangesStatusFromNewToCancelled(int _)
    {
        var order = await CreateOrderAsync();

        var cancelled = await Sut.CancelAsync(order.Id);

        Assert.Equal(OrderStatus.Cancelled, cancelled.Status);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task CancelAsync_ChangesStatusFromConfirmedToCancelled(int _)
    {
        var order = await CreateOrderAsync();
        await Sut.ConfirmAsync(order.Id);

        var cancelled = await Sut.CancelAsync(order.Id);

        Assert.Equal(OrderStatus.Cancelled, cancelled.Status);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task ConfirmAsync_WhenOrderIsCompleted_ThrowsInvalidOrderStatusTransitionException(int _)
    {
        var order = await CreateOrderAsync();
        await Sut.ConfirmAsync(order.Id);
        await Sut.ShipAsync(order.Id);
        await Sut.CompleteAsync(order.Id);

        await Assert.ThrowsAsync<InvalidOrderStatusTransitionException>(
            () => Sut.ConfirmAsync(order.Id));
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task CancelAsync_WhenOrderIsShipped_ThrowsInvalidOrderStatusTransitionException(int _)
    {
        var order = await CreateOrderAsync();
        await Sut.ConfirmAsync(order.Id);
        await Sut.ShipAsync(order.Id);

        await Assert.ThrowsAsync<InvalidOrderStatusTransitionException>(
            () => Sut.CancelAsync(order.Id));
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task FullLifecycle_CreateConfirmShipComplete_StatusCorrectAtEachStep(int _)
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
    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task MultiItemOrder_TotalAmountAndItemsCorrect(int _)
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
    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task MultiItemLifecycle_FullPath_TotalAmountAndStatusCorrect(int _)
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
