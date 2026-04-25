namespace FastIntegrationTests.Tests.Respawn.Orders;

/// <summary>
/// Тесты сервисного уровня: GetAll, GetById, Create для OrderService.
/// Каждый тест сбрасывает данные через Respawn (~1 мс), схема сохраняется.
/// </summary>
public class OrderServiceCrRespawnTests : RespawnServiceTestBase
{
    /// <summary>Создаёт новый экземпляр <see cref="OrderServiceCrRespawnTests"/>.</summary>
    /// <param name="fixture">Фикстура с контейнером и Respawner.</param>
    public OrderServiceCrRespawnTests(RespawnFixture fixture) : base(fixture) { }

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
    public async Task GetAllAsync_WhenNoOrders_ReturnsEmptyList(int _)
    {
        var result = await Sut.GetAllAsync();

        Assert.Empty(result);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetAllAsync_WhenOrdersExist_ReturnsAllOrders(int _)
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

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetByIdAsync_WhenOrderExists_ReturnsOrderWithItems(int _)
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

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetByIdAsync_WhenOrderNotFound_ThrowsNotFoundException(int _)
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Sut.GetByIdAsync(999));
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task CreateAsync_CalculatesTotalAmountCorrectly(int _)
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

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task CreateAsync_SetsUnitPriceFromCurrentProductPrice(int _)
    {
        var product = await _products.CreateAsync(new CreateProductRequest { Name = "Товар", Price = 999m });

        var order = await Sut.CreateAsync(new CreateOrderRequest
        {
            Items = new List<OrderItemRequest> { new() { ProductId = product.Id, Quantity = 1 } }
        });

        Assert.Equal(999m, order.Items[0].UnitPrice);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task CreateAsync_WhenProductNotFound_ThrowsNotFoundException(int _)
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Sut.CreateAsync(new CreateOrderRequest
        {
            Items = new List<OrderItemRequest> { new() { ProductId = 999, Quantity = 1 } }
        }));
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task CreateAsync_NewOrderHasStatusNew(int _)
    {
        var product = await _products.CreateAsync(new CreateProductRequest { Name = "Товар", Price = 100m });

        var order = await Sut.CreateAsync(new CreateOrderRequest
        {
            Items = new List<OrderItemRequest> { new() { ProductId = product.Id, Quantity = 1 } }
        });

        Assert.Equal(OrderStatus.New, order.Status);
    }
}
