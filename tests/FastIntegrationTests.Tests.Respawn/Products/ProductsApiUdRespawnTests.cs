namespace FastIntegrationTests.Tests.Respawn.Products;

/// <summary>
/// Тесты HTTP-уровня: Update, Delete для ProductsController.
/// Каждый тест сбрасывает данные через Respawn (~1 мс), схема и TestServer сохраняются.
/// </summary>
public class ProductsApiUdRespawnTests : RespawnApiTestBase
{
    /// <summary>Создаёт новый экземпляр <see cref="ProductsApiUdRespawnTests"/>.</summary>
    /// <param name="fixture">Фикстура с контейнером и Respawner.</param>
    public ProductsApiUdRespawnTests(RespawnApiFixture fixture) : base(fixture) { }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task Update_WhenProductExists_Returns200WithUpdatedFields(int _)
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

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task Update_WhenProductNotFound_Returns404(int _)
    {
        var request = new UpdateProductRequest { Name = "Название", Description = string.Empty, Price = 100m };

        var response = await Client.PutAsJsonAsync("/api/products/999", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task Delete_WhenProductExists_Returns204(int _)
    {
        var created = await CreateProductAsync("Удаляемый", 100m);

        var response = await Client.DeleteAsync($"/api/products/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task Delete_WhenProductNotFound_Returns404(int _)
    {
        var response = await Client.DeleteAsync("/api/products/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Создаёт несколько товаров через API, проверяет GetAll и GetById каждого.
    /// </summary>
    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task CreateMultiple_GetAll_GetByIdEach_ReturnsConsistentData(int _)
    {
        var a = await CreateProductAsync("Товар А", 100m);
        var b = await CreateProductAsync("Товар Б", 200m);
        var c = await CreateProductAsync("Товар В", 300m);

        var all = await Client.GetAsync("/api/products");
        var list = await all.Content.ReadFromJsonAsync<List<ProductDto>>();
        Assert.Equal(3, list!.Count);

        var fa = await (await Client.GetAsync($"/api/products/{a.Id}")).Content.ReadFromJsonAsync<ProductDto>();
        var fb = await (await Client.GetAsync($"/api/products/{b.Id}")).Content.ReadFromJsonAsync<ProductDto>();
        var fc = await (await Client.GetAsync($"/api/products/{c.Id}")).Content.ReadFromJsonAsync<ProductDto>();
        Assert.Equal("Товар А", fa!.Name);
        Assert.Equal("Товар Б", fb!.Name);
        Assert.Equal("Товар В", fc!.Name);

        // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
        for (var i = 0; i < 4; i++)
        {
            var extra = await CreateProductAsync($"Доп {i}", 500m + i * 50m);
            await Client.GetAsync($"/api/products/{extra.Id}");
        }
        await Client.GetAsync("/api/products");
    }

    /// <summary>
    /// Создаёт товар, обновляет через PUT, проверяет GET, удаляет — полный HTTP-цикл.
    /// </summary>
    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task CreateUpdateDelete_VerifyEachStep_AllPersist(int _)
    {
        var created = await CreateProductAsync("Монитор", 20_000m);

        var putResp = await Client.PutAsJsonAsync($"/api/products/{created.Id}",
            new UpdateProductRequest { Name = "Монитор 4K", Description = "UHD", Price = 25_000m });
        Assert.Equal(HttpStatusCode.OK, putResp.StatusCode);
        var updated = await putResp.Content.ReadFromJsonAsync<ProductDto>();
        Assert.Equal("Монитор 4K", updated!.Name);

        var getResp = await Client.GetAsync($"/api/products/{created.Id}");
        var fetched = await getResp.Content.ReadFromJsonAsync<ProductDto>();
        Assert.Equal("Монитор 4K", fetched!.Name);

        var delResp = await Client.DeleteAsync($"/api/products/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delResp.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync($"/api/products/{created.Id}")).StatusCode);

        // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
        for (var i = 0; i < 4; i++)
        {
            var extra = await CreateProductAsync($"Доп {i}", 1_000m + i * 100m);
            await Client.PutAsJsonAsync($"/api/products/{extra.Id}",
                new UpdateProductRequest { Name = $"Доп {i} v2", Price = 1_100m + i * 100m });
            await Client.GetAsync($"/api/products/{extra.Id}");
        }
        await Client.GetAsync("/api/products");
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
