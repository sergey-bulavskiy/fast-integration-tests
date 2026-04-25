namespace FastIntegrationTests.Tests.IntegreSQL.Orders;

/// <summary>
/// Тесты сервисного уровня: GetAll, GetById, Create для OrderService.
/// Каждый тест получает изолированный клон БД через IntegreSQL (~5 мс).
/// </summary>
public class OrderServiceCrTests : AppServiceTestBase
{
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
}
