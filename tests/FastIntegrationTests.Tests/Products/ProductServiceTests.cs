namespace FastIntegrationTests.Tests.Products;

/// <summary>
/// Интеграционные тесты сервисного уровня для ProductService.
/// Каждый тест работает с изолированной базой данных.
/// </summary>
[Collection("ProductsService")]
public class ProductServiceTests : ServiceTestBase
{
    /// <param name="fixture">Контейнер PostgreSQL/MSSQL, общий для коллекции.</param>
    public ProductServiceTests(ContainerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task GetAllAsync_WhenNoProducts_ReturnsEmptyList()
    {
        var result = await ProductService.GetAllAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_WhenProductsExist_ReturnsAllProducts()
    {
        await ProductService.CreateAsync(new CreateProductRequest { Name = "Товар 1", Description = "Описание 1", Price = 100m });
        await ProductService.CreateAsync(new CreateProductRequest { Name = "Товар 2", Description = "Описание 2", Price = 200m });

        var result = await ProductService.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductExists_ReturnsProduct()
    {
        var created = await ProductService.CreateAsync(new CreateProductRequest { Name = "Ноутбук", Description = "Core i9", Price = 50_000m });

        var result = await ProductService.GetByIdAsync(created.Id);

        Assert.Equal(created.Id, result.Id);
        Assert.Equal("Ноутбук", result.Name);
        Assert.Equal("Core i9", result.Description);
        Assert.Equal(50_000m, result.Price);
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductNotFound_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => ProductService.GetByIdAsync(999));
    }

    [Fact]
    public async Task CreateAsync_PersistsProductAndReturnsWithAssignedId()
    {
        var request = new CreateProductRequest { Name = "Мышь", Description = "Беспроводная", Price = 2_500m };

        var result = await ProductService.CreateAsync(request);

        Assert.True(result.Id > 0);
        Assert.Equal("Мышь", result.Name);
        Assert.Equal("Беспроводная", result.Description);
        Assert.Equal(2_500m, result.Price);
    }

    [Fact]
    public async Task CreateAsync_SetsCreatedAtAutomatically()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var result = await ProductService.CreateAsync(new CreateProductRequest { Name = "Клавиатура", Price = 3_000m });

        var after = DateTime.UtcNow.AddSeconds(1);
        Assert.InRange(result.CreatedAt, before, after);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesProductFieldsInDatabase()
    {
        var created = await ProductService.CreateAsync(new CreateProductRequest { Name = "Старое название", Price = 1_000m });
        var updateRequest = new UpdateProductRequest { Name = "Новое название", Description = "Новое описание", Price = 1_500m };

        var updated = await ProductService.UpdateAsync(created.Id, updateRequest);

        Assert.Equal("Новое название", updated.Name);
        Assert.Equal("Новое описание", updated.Description);
        Assert.Equal(1_500m, updated.Price);

        // Проверяем сохранение в БД через повторный запрос
        var fetched = await ProductService.GetByIdAsync(created.Id);
        Assert.Equal("Новое название", fetched.Name);
        Assert.Equal(1_500m, fetched.Price);
    }

    [Fact]
    public async Task UpdateAsync_WhenProductNotFound_ThrowsNotFoundException()
    {
        var request = new UpdateProductRequest { Name = "Название", Description = string.Empty, Price = 100m };

        await Assert.ThrowsAsync<NotFoundException>(() => ProductService.UpdateAsync(999, request));
    }

    [Fact]
    public async Task DeleteAsync_RemovesProductFromDatabase()
    {
        var created = await ProductService.CreateAsync(new CreateProductRequest { Name = "Временный товар", Price = 500m });

        await ProductService.DeleteAsync(created.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => ProductService.GetByIdAsync(created.Id));
    }

    [Fact]
    public async Task DeleteAsync_WhenProductNotFound_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => ProductService.DeleteAsync(999));
    }

    [Fact]
    public async Task DeleteAsync_WhenProductHasOrderItems_ThrowsDbUpdateException()
    {
        // Создаём товар и заказ с этим товаром
        var product = await ProductService.CreateAsync(new CreateProductRequest { Name = "Товар в заказе", Price = 1_000m });
        await OrderService.CreateAsync(new CreateOrderRequest
        {
            Items = new List<OrderItemRequest> { new() { ProductId = product.Id, Quantity = 1 } }
        });

        // FK Restrict: нельзя удалить товар, на который ссылаются позиции заказа
        await Assert.ThrowsAsync<DbUpdateException>(() => ProductService.DeleteAsync(product.Id));
    }
}
