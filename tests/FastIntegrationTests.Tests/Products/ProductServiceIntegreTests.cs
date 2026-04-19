namespace FastIntegrationTests.Tests.Products;

/// <summary>
/// Дымовые тесты сервисного уровня через IntegreSQL.
/// Проверяют работу шаблонного клонирования БД.
/// </summary>
public class ProductServiceIntegreTests : AppServiceTestBase
{
    /// <summary>
    /// GetAllAsync при пустой базе возвращает пустой список.
    /// </summary>
    [Fact]
    public async Task GetAllAsync_WhenNoProducts_ReturnsEmptyList()
    {
        var result = await ProductService.GetAllAsync();

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

        var result = await ProductService.CreateAsync(request);

        Assert.True(result.Id > 0);
        Assert.Equal("Ноутбук", result.Name);
        Assert.Equal("Core i9", result.Description);
        Assert.Equal(50_000m, result.Price);
    }
}
