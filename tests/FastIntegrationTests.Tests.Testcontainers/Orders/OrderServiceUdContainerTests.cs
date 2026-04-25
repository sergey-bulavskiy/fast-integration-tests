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
