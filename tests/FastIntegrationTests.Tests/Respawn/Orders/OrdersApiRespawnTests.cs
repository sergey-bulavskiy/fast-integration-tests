namespace FastIntegrationTests.Tests.Respawn.Orders;

/// <summary>
/// Интеграционные тесты HTTP-уровня для OrdersController.
/// Проверяют HTTP-статусы, тела ответов и полный жизненный цикл заказа.
/// </summary>
public class OrdersApiRespawnTests : RespawnApiTestBase
{
    /// <summary>Создаёт новый экземпляр <see cref="OrdersApiRespawnTests"/>.</summary>
    /// <param name="fixture">Фикстура с контейнером и Respawner.</param>
    public OrdersApiRespawnTests(RespawnApiFixture fixture) : base(fixture) { }

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

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task Confirm_WhenOrderIsNew_Returns200WithConfirmedStatus(int _)
    {
        var order = await CreateOrderWithProductAsync();

        var response = await Client.PostAsync($"/api/orders/{order.Id}/confirm", null);
        var confirmed = await response.Content.ReadFromJsonAsync<OrderDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(OrderStatus.Confirmed, confirmed!.Status);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task Ship_WhenOrderIsConfirmed_Returns200WithShippedStatus(int _)
    {
        var order = await CreateOrderWithProductAsync();
        await Client.PostAsync($"/api/orders/{order.Id}/confirm", null);

        var response = await Client.PostAsync($"/api/orders/{order.Id}/ship", null);
        var shipped = await response.Content.ReadFromJsonAsync<OrderDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(OrderStatus.Shipped, shipped!.Status);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task Complete_WhenOrderIsShipped_Returns200WithCompletedStatus(int _)
    {
        var order = await CreateOrderWithProductAsync();
        await Client.PostAsync($"/api/orders/{order.Id}/confirm", null);
        await Client.PostAsync($"/api/orders/{order.Id}/ship", null);

        var response = await Client.PostAsync($"/api/orders/{order.Id}/complete", null);
        var completed = await response.Content.ReadFromJsonAsync<OrderDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(OrderStatus.Completed, completed!.Status);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task Cancel_WhenOrderIsNew_Returns200WithCancelledStatus(int _)
    {
        var order = await CreateOrderWithProductAsync();

        var response = await Client.PostAsync($"/api/orders/{order.Id}/cancel", null);
        var cancelled = await response.Content.ReadFromJsonAsync<OrderDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(OrderStatus.Cancelled, cancelled!.Status);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task Cancel_WhenOrderIsConfirmed_Returns200WithCancelledStatus(int _)
    {
        var order = await CreateOrderWithProductAsync();
        await Client.PostAsync($"/api/orders/{order.Id}/confirm", null);

        var response = await Client.PostAsync($"/api/orders/{order.Id}/cancel", null);
        var cancelled = await response.Content.ReadFromJsonAsync<OrderDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(OrderStatus.Cancelled, cancelled!.Status);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task Confirm_WhenOrderNotFound_Returns404(int _)
    {
        var response = await Client.PostAsync("/api/orders/999/confirm", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task Ship_WhenOrderIsNew_InvalidTransition_Returns400(int _)
    {
        var order = await CreateOrderWithProductAsync();

        // New → Shipped недопустимо: пропущен Confirmed
        var response = await Client.PostAsync($"/api/orders/{order.Id}/ship", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task Cancel_WhenOrderIsShipped_InvalidTransition_Returns400(int _)
    {
        var order = await CreateOrderWithProductAsync();
        await Client.PostAsync($"/api/orders/{order.Id}/confirm", null);
        await Client.PostAsync($"/api/orders/{order.Id}/ship", null);

        // Shipped → Cancelled недопустимо
        var response = await Client.PostAsync($"/api/orders/{order.Id}/cancel", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task CreateThenGetById_OrderItemsMatchRequest(int _)
    {
        var product = await CreateProductAsync("Видеокарта", 40_000m);
        var createRequest = new CreateOrderRequest
        {
            Items = new List<OrderItemRequest> { new() { ProductId = product.Id, Quantity = 2 } }
        };

        var createResponse = await Client.PostAsJsonAsync("/api/orders", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<OrderDto>();

        var getResponse = await Client.GetAsync($"/api/orders/{created!.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<OrderDto>();

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Single(fetched!.Items);
        Assert.Equal(product.Id, fetched.Items[0].ProductId);
        Assert.Equal(2, fetched.Items[0].Quantity);
        Assert.Equal(40_000m, fetched.Items[0].UnitPrice);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task FullLifecycle_CreateConfirmShipCompleteGetById_StatusIsCompleted(int _)
    {
        var order = await CreateOrderWithProductAsync();

        await Client.PostAsync($"/api/orders/{order.Id}/confirm", null);
        await Client.PostAsync($"/api/orders/{order.Id}/ship", null);
        await Client.PostAsync($"/api/orders/{order.Id}/complete", null);

        var response = await Client.GetAsync($"/api/orders/{order.Id}");
        var completed = await response.Content.ReadFromJsonAsync<OrderDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(OrderStatus.Completed, completed!.Status);
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
