namespace FastIntegrationTests.Tests.IntegreSQL.Orders;

/// <summary>
/// Тесты HTTP-уровня: GetAll, GetById, Create для OrdersController.
/// Каждый тест получает изолированный клон БД через IntegreSQL (~5 мс) и отдельный TestServer.
/// </summary>
public class OrdersApiCrTests : ComponentTestBase
{
    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetAll_WhenNoOrders_Returns200WithEmptyArray(int _)
    {
        var response = await Client.GetAsync("/api/orders");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var orders = await response.Content.ReadFromJsonAsync<List<OrderDto>>();
        Assert.Empty(orders!);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetAll_WhenOrdersExist_Returns200WithOrders(int _)
    {
        await CreateOrderWithProductAsync();
        await CreateOrderWithProductAsync();

        var response = await Client.GetAsync("/api/orders");
        var orders = await response.Content.ReadFromJsonAsync<List<OrderDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, orders!.Count);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetById_WhenOrderExists_Returns200WithItems(int _)
    {
        var created = await CreateOrderWithProductAsync();

        var response = await Client.GetAsync($"/api/orders/{created.Id}");
        var order = await response.Content.ReadFromJsonAsync<OrderDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(created.Id, order!.Id);
        Assert.Single(order.Items);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetById_WhenOrderNotFound_Returns404(int _)
    {
        var response = await Client.GetAsync("/api/orders/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task Create_ValidRequest_Returns201WithCalculatedTotalAmount(int _)
    {
        var product = await CreateProductAsync("Процессор", 15_000m);
        var request = new CreateOrderRequest
        {
            Items = new List<OrderItemRequest> { new() { ProductId = product.Id, Quantity = 2 } }
        };

        var response = await Client.PostAsJsonAsync("/api/orders", request);
        var order = await response.Content.ReadFromJsonAsync<OrderDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.True(order!.Id > 0);
        Assert.Equal(30_000m, order.TotalAmount); // 2 * 15000
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task Create_WhenProductNotFound_Returns404(int _)
    {
        var request = new CreateOrderRequest
        {
            Items = new List<OrderItemRequest> { new() { ProductId = 999, Quantity = 1 } }
        };

        var response = await Client.PostAsJsonAsync("/api/orders", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Создаёт заказ с тремя позициями через API — проверяет итоговую сумму и состав.
    /// </summary>
    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task MultiItemOrder_TotalAmountAndItemsCorrect(int _)
    {
        var p1 = await CreateProductAsync("Телефон", 30_000m);
        var p2 = await CreateProductAsync("Чехол", 500m);
        var p3 = await CreateProductAsync("Зарядка", 1_500m);

        var createResp = await Client.PostAsJsonAsync("/api/orders", new CreateOrderRequest
        {
            Items = new List<OrderItemRequest>
            {
                new() { ProductId = p1.Id, Quantity = 1 },
                new() { ProductId = p2.Id, Quantity = 2 },
                new() { ProductId = p3.Id, Quantity = 1 },
            }
        });
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        var order = await createResp.Content.ReadFromJsonAsync<OrderDto>();
        Assert.Equal(32_500m, order!.TotalAmount);
        Assert.Equal(3, order.Items.Count);

        var fetched = await (await Client.GetAsync($"/api/orders/{order.Id}")).Content.ReadFromJsonAsync<OrderDto>();
        Assert.Equal(32_500m, fetched!.TotalAmount);

        // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
        var extraProduct = await CreateProductAsync("Доп товар", 100m);
        for (var i = 0; i < 3; i++)
        {
            var extra = await Client.PostAsJsonAsync("/api/orders", new CreateOrderRequest
            {
                Items = new List<OrderItemRequest> { new() { ProductId = extraProduct.Id, Quantity = 1 } }
            });
            var extraOrder = await extra.Content.ReadFromJsonAsync<OrderDto>();
            await Client.GetAsync($"/api/orders/{extraOrder!.Id}");
        }
        await Client.GetAsync("/api/orders");
    }

    /// <summary>
    /// Создаёт заказ с тремя позициями, проводит полный lifecycle через API.
    /// </summary>
    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task MultiItemLifecycle_FullPath_TotalAmountAndStatusCorrect(int _)
    {
        var p1 = await CreateProductAsync("Ноутбук", 50_000m);
        var p2 = await CreateProductAsync("Мышь", 2_000m);
        var p3 = await CreateProductAsync("Клавиатура", 3_000m);

        var createResp = await Client.PostAsJsonAsync("/api/orders", new CreateOrderRequest
        {
            Items = new List<OrderItemRequest>
            {
                new() { ProductId = p1.Id, Quantity = 1 },
                new() { ProductId = p2.Id, Quantity = 1 },
                new() { ProductId = p3.Id, Quantity = 2 },
            }
        });
        var order = await createResp.Content.ReadFromJsonAsync<OrderDto>();
        Assert.Equal(58_000m, order!.TotalAmount);

        Assert.Equal(HttpStatusCode.OK, (await Client.PostAsync($"/api/orders/{order.Id}/confirm", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Client.PostAsync($"/api/orders/{order.Id}/ship", null)).StatusCode);
        var completedResp = await Client.PostAsync($"/api/orders/{order.Id}/complete", null);
        Assert.Equal(HttpStatusCode.OK, completedResp.StatusCode);
        var completed = await completedResp.Content.ReadFromJsonAsync<OrderDto>();
        Assert.Equal(OrderStatus.Completed, completed!.Status);

        var fetched = await (await Client.GetAsync($"/api/orders/{order.Id}")).Content.ReadFromJsonAsync<OrderDto>();
        Assert.Equal(OrderStatus.Completed, fetched!.Status);
        Assert.Equal(58_000m, fetched.TotalAmount);
        Assert.Equal(3, fetched.Items.Count);
    }

    // --- helpers ---

    /// <summary>
    /// Создаёт товар через API и возвращает его DTO.
    /// </summary>
    /// <param name="name">Название товара.</param>
    /// <param name="price">Цена товара.</param>
    /// <param name="ct">Токен отмены операции.</param>
    private async Task<ProductDto> CreateProductAsync(string name, decimal price, CancellationToken ct = default)
    {
        var response = await Client.PostAsJsonAsync("/api/products",
            new CreateProductRequest { Name = name, Price = price }, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductDto>(ct))!;
    }

    /// <summary>
    /// Создаёт товар и заказ с одной позицией через API, возвращает DTO заказа.
    /// </summary>
    /// <param name="ct">Токен отмены операции.</param>
    private async Task<OrderDto> CreateOrderWithProductAsync(CancellationToken ct = default)
    {
        var product = await CreateProductAsync("Товар", 100m, ct);
        var response = await Client.PostAsJsonAsync("/api/orders", new CreateOrderRequest
        {
            Items = new List<OrderItemRequest> { new() { ProductId = product.Id, Quantity = 1 } }
        }, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OrderDto>(ct))!;
    }
}
