namespace FastIntegrationTests.Tests.IntegreSQL.Products;

/// <summary>
/// Дымовые тесты сервисного уровня через IntegreSQL.
/// Проверяют работу шаблонного клонирования БД.
/// </summary>
public class ProductServiceIntegreTests : AppServiceTestBase
{
    private IProductService _productService = null!;

    /// <inheritdoc/>
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        var productRepo = new ProductRepository(Context);
        _productService = new ProductService(productRepo);
    }

    /// <summary>
    /// GetAllAsync при пустой базе возвращает пустой список.
    /// </summary>
    [Fact]
    public async Task GetAllAsync_WhenNoProducts_ReturnsEmptyList()
    {
        var result = await _productService.GetAllAsync();

        Assert.Empty(result);
    }

    /// <summary>
    /// CreateAsync сохраняет товар и возвращает его с присвоенным Id.
    /// </summary>
    [Fact]
    public async Task CreateAsync_PersistsProductAndReturnsWithAssignedId()
    {
        var request = new CreateProductRequest
        {
            Name = "Ноутбук",
            Description = "Core i9",
            Price = 50_000m
        };

        var result = await _productService.CreateAsync(request);

        Assert.True(result.Id > 0);
        Assert.Equal("Ноутбук", result.Name);
        Assert.Equal("Core i9", result.Description);
        Assert.Equal(50_000m, result.Price);
    }
}
