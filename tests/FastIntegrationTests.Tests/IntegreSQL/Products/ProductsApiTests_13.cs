namespace FastIntegrationTests.Tests.IntegreSQL.Products;

/// <summary>
/// Интеграционные тесты HTTP-уровня для ProductsController.
/// Каждый тест получает изолированный клон БД через IntegreSQL (~5 мс) и отдельный TestServer.
/// </summary>
public class ProductsApiTests_13 : ComponentTestBase
{
    /// <summary>
    /// GET /api/products при пустой базе возвращает 200 и пустой массив.
    /// </summary>
    [Fact]
    public async Task GetAll_WhenNoProducts_Returns200WithEmptyArray()
    {
        var response = await Client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();
        Assert.Empty(products!);
    }

    /// <summary>
    /// GET /api/products при наличии товаров возвращает 200 и список.
    /// </summary>
    [Fact]
    public async Task GetAll_WhenProductsExist_Returns200WithProducts()
    {
        await CreateProductAsync("Товар 1", 100m);
        await CreateProductAsync("Товар 2", 200m);

        var response = await Client.GetAsync("/api/products");
        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, products!.Count);
    }

    /// <summary>
    /// GET /api/products/{id} для существующего товара возвращает 200 и данные товара.
    /// </summary>
    [Fact]
    public async Task GetById_WhenProductExists_Returns200WithProduct()
    {
        var created = await CreateProductAsync("Ноутбук", 50_000m);

        var response = await Client.GetAsync($"/api/products/{created.Id}");
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(created.Id, product!.Id);
        Assert.Equal("Ноутбук", product.Name);
    }

    /// <summary>
    /// GET /api/products/{id} для несуществующего товара возвращает 404.
    /// </summary>
    [Fact]
    public async Task GetById_WhenProductNotFound_Returns404()
    {
        var response = await Client.GetAsync("/api/products/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// POST /api/products с валидными данными возвращает 201, заголовок Location и созданный товар.
    /// </summary>
    [Fact]
    public async Task Create_ValidRequest_Returns201WithLocationHeaderAndId()
    {
        var request = new CreateProductRequest { Name = "Монитор", Description = "4K", Price = 25_000m };

        var response = await Client.PostAsJsonAsync("/api/products", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.True(product!.Id > 0);
        Assert.Equal("Монитор", product.Name);
    }

    /// <summary>
    /// PUT /api/products/{id} для существующего товара возвращает 200 с обновлёнными полями.
    /// </summary>
    [Fact]
    public async Task Update_WhenProductExists_Returns200WithUpdatedFields()
    {
        var created = await CreateProductAsync("Старое", 100m);
        var updateRequest = new UpdateProductRequest { Name = "Новое", Description = "Обновлено", Price = 200m };

        var response = await Client.PutAsJsonAsync($"/api/products/{created.Id}", updateRequest);
        var updated = await response.Content.ReadFromJsonAsync<ProductDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Новое", updated!.Name);
        Assert.Equal("Обновлено", updated.Description);
        Assert.Equal(200m, updated.Price);
    }

    /// <summary>
    /// PUT /api/products/{id} для несуществующего товара возвращает 404.
    /// </summary>
    [Fact]
    public async Task Update_WhenProductNotFound_Returns404()
    {
        var request = new UpdateProductRequest { Name = "Название", Description = string.Empty, Price = 100m };

        var response = await Client.PutAsJsonAsync("/api/products/999", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// DELETE /api/products/{id} для существующего товара возвращает 204.
    /// </summary>
    [Fact]
    public async Task Delete_WhenProductExists_Returns204()
    {
        var created = await CreateProductAsync("Удаляемый", 100m);

        var response = await Client.DeleteAsync($"/api/products/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    /// <summary>
    /// DELETE /api/products/{id} для несуществующего товара возвращает 404.
    /// </summary>
    [Fact]
    public async Task Delete_WhenProductNotFound_Returns404()
    {
        var response = await Client.DeleteAsync("/api/products/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// POST затем GET возвращают идентичные данные товара.
    /// </summary>
    [Fact]
    public async Task CreateThenGetById_DataMatchesExactly()
    {
        var createRequest = new CreateProductRequest { Name = "Системный блок", Description = "Core i9", Price = 80_000m };
        var createResponse = await Client.PostAsJsonAsync("/api/products", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductDto>();

        var getResponse = await Client.GetAsync($"/api/products/{created!.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<ProductDto>();

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Equal(created.Id, fetched!.Id);
        Assert.Equal("Системный блок", fetched.Name);
        Assert.Equal("Core i9", fetched.Description);
        Assert.Equal(80_000m, fetched.Price);
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
}
