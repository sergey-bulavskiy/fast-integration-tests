namespace FastIntegrationTests.Tests.NUnit.IntegreSQL.Products;

/// <summary>
/// Тесты HTTP-уровня: GetAll, GetById, Create, Update, Delete для ProductsController.
/// Каждый тест получает изолированный клон БД через IntegreSQL (~5 мс) и отдельный TestServer.
/// </summary>
[TestFixture]
public class ProductsApiTests : ComponentTestBase
{
    [Test]
    public async Task GetAll_WhenNoProducts_Returns200WithEmptyArray()
    {
        var response = await Client.GetAsync("/api/products");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();
        Assert.That(products, Is.Empty);
    }

    [Test]
    public async Task GetAll_WhenProductsExist_Returns200WithProducts()
    {
        await CreateProductAsync("Товар 1", 100m);
        await CreateProductAsync("Товар 2", 200m);

        var response = await Client.GetAsync("/api/products");
        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(products!.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task GetById_WhenProductExists_Returns200WithProduct()
    {
        var created = await CreateProductAsync("Ноутбук", 50_000m);

        var response = await Client.GetAsync($"/api/products/{created.Id}");
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(product!.Id, Is.EqualTo(created.Id));
        Assert.That(product.Name, Is.EqualTo("Ноутбук"));
    }

    [Test]
    public async Task GetById_WhenProductNotFound_Returns404()
    {
        var response = await Client.GetAsync("/api/products/999");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Create_ValidRequest_Returns201WithLocationHeaderAndId()
    {
        var request = new CreateProductRequest { Name = "Монитор", Description = "4K", Price = 25_000m };

        var response = await Client.PostAsJsonAsync("/api/products", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(response.Headers.Location, Is.Not.Null);
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.That(product!.Id, Is.GreaterThan(0));
        Assert.That(product.Name, Is.EqualTo("Монитор"));
    }

    [Test]
    public async Task CreateThenGetById_DataMatchesExactly()
    {
        var createRequest = new CreateProductRequest { Name = "Системный блок", Description = "Core i9", Price = 80_000m };
        var createResponse = await Client.PostAsJsonAsync("/api/products", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductDto>();

        var getResponse = await Client.GetAsync($"/api/products/{created!.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<ProductDto>();

        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(fetched!.Id, Is.EqualTo(created.Id));
        Assert.That(fetched.Name, Is.EqualTo("Системный блок"));
        Assert.That(fetched.Description, Is.EqualTo("Core i9"));
        Assert.That(fetched.Price, Is.EqualTo(80_000m));
    }

    [Test]
    public async Task Update_WhenProductExists_Returns200WithUpdatedFields()
    {
        var created = await CreateProductAsync("Старое", 100m);
        var updateRequest = new UpdateProductRequest { Name = "Новое", Description = "Обновлено", Price = 200m };

        var response = await Client.PutAsJsonAsync($"/api/products/{created.Id}", updateRequest);
        var updated = await response.Content.ReadFromJsonAsync<ProductDto>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(updated!.Name, Is.EqualTo("Новое"));
        Assert.That(updated.Description, Is.EqualTo("Обновлено"));
        Assert.That(updated.Price, Is.EqualTo(200m));
    }

    [Test]
    public async Task Update_WhenProductNotFound_Returns404()
    {
        var request = new UpdateProductRequest { Name = "Название", Description = string.Empty, Price = 100m };

        var response = await Client.PutAsJsonAsync("/api/products/999", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Delete_WhenProductExists_Returns204()
    {
        var created = await CreateProductAsync("Удаляемый", 100m);

        var response = await Client.DeleteAsync($"/api/products/{created.Id}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test]
    public async Task Delete_WhenProductNotFound_Returns404()
    {
        var response = await Client.DeleteAsync("/api/products/999");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// Создаёт несколько товаров через API, проверяет GetAll и GetById каждого.
    /// </summary>
    [Test]
    public async Task CreateMultiple_GetAll_GetByIdEach_ReturnsConsistentData()
    {
        var a = await CreateProductAsync("Товар А", 100m);
        var b = await CreateProductAsync("Товар Б", 200m);
        var c = await CreateProductAsync("Товар В", 300m);

        var all = await Client.GetAsync("/api/products");
        var list = await all.Content.ReadFromJsonAsync<List<ProductDto>>();
        Assert.That(list!.Count, Is.EqualTo(3));

        var fa = await (await Client.GetAsync($"/api/products/{a.Id}")).Content.ReadFromJsonAsync<ProductDto>();
        var fb = await (await Client.GetAsync($"/api/products/{b.Id}")).Content.ReadFromJsonAsync<ProductDto>();
        var fc = await (await Client.GetAsync($"/api/products/{c.Id}")).Content.ReadFromJsonAsync<ProductDto>();
        Assert.That(fa!.Name, Is.EqualTo("Товар А"));
        Assert.That(fb!.Name, Is.EqualTo("Товар Б"));
        Assert.That(fc!.Name, Is.EqualTo("Товар В"));

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
    [Test]
    public async Task CreateUpdateDelete_VerifyEachStep_AllPersist()
    {
        var created = await CreateProductAsync("Монитор", 20_000m);

        var putResp = await Client.PutAsJsonAsync($"/api/products/{created.Id}",
            new UpdateProductRequest { Name = "Монитор 4K", Description = "UHD", Price = 25_000m });
        Assert.That(putResp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var updated = await putResp.Content.ReadFromJsonAsync<ProductDto>();
        Assert.That(updated!.Name, Is.EqualTo("Монитор 4K"));

        var getResp = await Client.GetAsync($"/api/products/{created.Id}");
        var fetched = await getResp.Content.ReadFromJsonAsync<ProductDto>();
        Assert.That(fetched!.Name, Is.EqualTo("Монитор 4K"));

        var delResp = await Client.DeleteAsync($"/api/products/{created.Id}");
        Assert.That(delResp.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That((await Client.GetAsync($"/api/products/{created.Id}")).StatusCode,
            Is.EqualTo(HttpStatusCode.NotFound));

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
