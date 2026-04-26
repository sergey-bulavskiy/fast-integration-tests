namespace FastIntegrationTests.Tests.Testcontainers.Orders;

/// <summary>
/// Тесты HTTP-уровня: статусные переходы и сквозные сценарии для OrdersController.
/// Каждый тест создаёт изолированную БД в реальном контейнере PostgreSQL и отдельный TestServer.
/// </summary>
public class OrdersApiUdContainerTests : ContainerApiTestBase
{
    /// <summary>
    /// Создаёт новый экземпляр <see cref="OrdersApiUdContainerTests"/>.
    /// </summary>
    /// <param name="fixture">Запущенный контейнер с СУБД.</param>
    public OrdersApiUdContainerTests(ContainerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Confirm_WhenOrderIsNew_Returns200WithConfirmedStatus()
    {
        var order = await CreateOrderWithProductAsync();

        var response = await Client.PostAsync($"/api/orders/{order.Id}/confirm", null);
        var confirmed = await response.Content.ReadFromJsonAsync<OrderDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(OrderStatus.Confirmed, confirmed!.Status);
    }

    [Fact]
    public async Task Ship_WhenOrderIsConfirmed_Returns200WithShippedStatus()
    {
        var order = await CreateOrderWithProductAsync();
        await Client.PostAsync($"/api/orders/{order.Id}/confirm", null);

        var response = await Client.PostAsync($"/api/orders/{order.Id}/ship", null);
        var shipped = await response.Content.ReadFromJsonAsync<OrderDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(OrderStatus.Shipped, shipped!.Status);
    }

    [Fact]
    public async Task Complete_WhenOrderIsShipped_Returns200WithCompletedStatus()
    {
        var order = await CreateOrderWithProductAsync();
        await Client.PostAsync($"/api/orders/{order.Id}/confirm", null);
        await Client.PostAsync($"/api/orders/{order.Id}/ship", null);

        var response = await Client.PostAsync($"/api/orders/{order.Id}/complete", null);
        var completed = await response.Content.ReadFromJsonAsync<OrderDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(OrderStatus.Completed, completed!.Status);
    }

    [Fact]
    public async Task Cancel_WhenOrderIsNew_Returns200WithCancelledStatus()
    {
        var order = await CreateOrderWithProductAsync();

        var response = await Client.PostAsync($"/api/orders/{order.Id}/cancel", null);
        var cancelled = await response.Content.ReadFromJsonAsync<OrderDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(OrderStatus.Cancelled, cancelled!.Status);
    }

    [Fact]
    public async Task Cancel_WhenOrderIsConfirmed_Returns200WithCancelledStatus()
    {
        var order = await CreateOrderWithProductAsync();
        await Client.PostAsync($"/api/orders/{order.Id}/confirm", null);

        var response = await Client.PostAsync($"/api/orders/{order.Id}/cancel", null);
        var cancelled = await response.Content.ReadFromJsonAsync<OrderDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(OrderStatus.Cancelled, cancelled!.Status);
    }

    [Fact]
    public async Task Confirm_WhenOrderNotFound_Returns404()
    {
        var response = await Client.PostAsync("/api/orders/999/confirm", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Ship_WhenOrderIsNew_InvalidTransition_Returns400()
    {
        var order = await CreateOrderWithProductAsync();

        // New → Shipped недопустимо: пропущен Confirmed
        var response = await Client.PostAsync($"/api/orders/{order.Id}/ship", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_WhenOrderIsShipped_InvalidTransition_Returns400()
    {
        var order = await CreateOrderWithProductAsync();
        await Client.PostAsync($"/api/orders/{order.Id}/confirm", null);
        await Client.PostAsync($"/api/orders/{order.Id}/ship", null);

        // Shipped → Cancelled недопустимо
        var response = await Client.PostAsync($"/api/orders/{order.Id}/cancel", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateThenGetById_OrderItemsMatchRequest()
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

    [Fact]
    public async Task FullLifecycle_CreateConfirmShipCompleteGetById_StatusIsCompleted()
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

    /// <summary>
    /// Создаёт заказ с тремя позициями через API — проверяет итоговую сумму и состав.
    /// </summary>
    [Fact]
    public async Task MultiItemOrder_TotalAmountAndItemsCorrect()
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
    [Fact]
    public async Task MultiItemLifecycle_FullPath_TotalAmountAndStatusCorrect()
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
