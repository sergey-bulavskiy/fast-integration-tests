namespace FastIntegrationTests.Tests.Respawn.Products;

/// <summary>
/// Тесты HTTP-уровня: GetAll, GetById, Create для ProductsController.
/// Каждый тест сбрасывает данные через Respawn (~1 мс), схема и TestServer сохраняются.
/// </summary>
public class ProductsApiCrRespawnTests : RespawnApiTestBase
{
    /// <summary>Создаёт новый экземпляр <see cref="ProductsApiCrRespawnTests"/>.</summary>
    /// <param name="fixture">Фикстура с контейнером и Respawner.</param>
    public ProductsApiCrRespawnTests(RespawnApiFixture fixture) : base(fixture) { }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetAll_WhenNoProducts_Returns200WithEmptyArray(int _)
    {
        var response = await Client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();
        Assert.Empty(products!);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetAll_WhenProductsExist_Returns200WithProducts(int _)
    {
        await CreateProductAsync("Товар 1", 100m);
        await CreateProductAsync("Товар 2", 200m);

        var response = await Client.GetAsync("/api/products");
        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, products!.Count);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetById_WhenProductExists_Returns200WithProduct(int _)
    {
        var created = await CreateProductAsync("Ноутбук", 50_000m);

        var response = await Client.GetAsync($"/api/products/{created.Id}");
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(created.Id, product!.Id);
        Assert.Equal("Ноутбук", product.Name);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetById_WhenProductNotFound_Returns404(int _)
    {
        var response = await Client.GetAsync("/api/products/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task Create_ValidRequest_Returns201WithLocationHeaderAndId(int _)
    {
        var request = new CreateProductRequest { Name = "Монитор", Description = "4K", Price = 25_000m };

        var response = await Client.PostAsJsonAsync("/api/products", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.True(product!.Id > 0);
        Assert.Equal("Монитор", product.Name);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task CreateThenGetById_DataMatchesExactly(int _)
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
