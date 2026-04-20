namespace FastIntegrationTests.Tests.IntegreSQL.Products;

/// <summary>
/// Дымовые HTTP-тесты через IntegreSQL.
/// Проверяют работу ComponentTestBase с реальным HTTP-клиентом.
/// </summary>
public class ProductsApiIntegreTests : ComponentTestBase
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
    /// POST /api/products с валидными данными возвращает 201 и созданный товар.
    /// </summary>
    [Fact]
    public async Task Create_ValidRequest_Returns201WithProduct()
    {
        var request = new CreateProductRequest
        {
            Name = "Монитор",
            Description = "4K",
            Price = 25_000m
        };

        var response = await Client.PostAsJsonAsync("/api/products", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.True(product!.Id > 0);
        Assert.Equal("Монитор", product.Name);
    }
}
