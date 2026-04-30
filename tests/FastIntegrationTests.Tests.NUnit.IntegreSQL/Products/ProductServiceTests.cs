namespace FastIntegrationTests.Tests.NUnit.IntegreSQL.Products;

/// <summary>
/// Тесты сервисного уровня: GetAll, GetById, Create, Update, Delete для ProductService.
/// Каждый тест получает изолированный клон БД через IntegreSQL (~5 мс).
/// </summary>
[TestFixture]
public class ProductServiceTests : AppServiceTestBase
{
    private IProductService _sut = null!;
    private IOrderService _orders = null!;

    [SetUp]
    public void SetUpServices()
    {
        var productRepo = new ProductRepository(Context);
        var orderRepo = new OrderRepository(Context);
        _sut = new ProductService(productRepo);
        _orders = new OrderService(orderRepo, productRepo);
    }

    [Test]
    public async Task GetAllAsync_WhenNoProducts_ReturnsEmptyList()
    {
        var result = await _sut.GetAllAsync();
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetAllAsync_WhenProductsExist_ReturnsAllProducts()
    {
        await _sut.CreateAsync(new CreateProductRequest { Name = "Товар 1", Description = "Описание 1", Price = 100m });
        await _sut.CreateAsync(new CreateProductRequest { Name = "Товар 2", Description = "Описание 2", Price = 200m });
        var result = await _sut.GetAllAsync();
        Assert.That(result.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task GetByIdAsync_WhenProductExists_ReturnsProduct()
    {
        var created = await _sut.CreateAsync(new CreateProductRequest { Name = "Ноутбук", Description = "Core i9", Price = 50_000m });
        var result = await _sut.GetByIdAsync(created.Id);
        Assert.That(result.Id, Is.EqualTo(created.Id));
        Assert.That(result.Name, Is.EqualTo("Ноутбук"));
        Assert.That(result.Description, Is.EqualTo("Core i9"));
        Assert.That(result.Price, Is.EqualTo(50_000m));
    }

    [Test]
    public async Task GetByIdAsync_WhenProductNotFound_ThrowsNotFoundException()
    {
        await Assert.ThatAsync(() => _sut.GetByIdAsync(999), Throws.TypeOf<NotFoundException>());
    }

    [Test]
    public async Task CreateAsync_PersistsProductAndReturnsWithAssignedId()
    {
        var request = new CreateProductRequest { Name = "Мышь", Description = "Беспроводная", Price = 2_500m };
        var result = await _sut.CreateAsync(request);
        Assert.That(result.Id, Is.GreaterThan(0));
        Assert.That(result.Name, Is.EqualTo("Мышь"));
        Assert.That(result.Description, Is.EqualTo("Беспроводная"));
        Assert.That(result.Price, Is.EqualTo(2_500m));
    }

    [Test]
    public async Task CreateAsync_SetsCreatedAtAutomatically()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var result = await _sut.CreateAsync(new CreateProductRequest { Name = "Клавиатура", Price = 3_000m });
        var after = DateTime.UtcNow.AddSeconds(1);
        Assert.That(result.CreatedAt, Is.InRange(before, after));
    }

    [Test]
    public async Task UpdateAsync_UpdatesProductFieldsInDatabase()
    {
        var created = await _sut.CreateAsync(new CreateProductRequest { Name = "Старое название", Price = 1_000m });
        var updateRequest = new UpdateProductRequest { Name = "Новое название", Description = "Новое описание", Price = 1_500m };
        var updated = await _sut.UpdateAsync(created.Id, updateRequest);
        Assert.That(updated.Name, Is.EqualTo("Новое название"));
        Assert.That(updated.Description, Is.EqualTo("Новое описание"));
        Assert.That(updated.Price, Is.EqualTo(1_500m));
        var fetched = await _sut.GetByIdAsync(created.Id);
        Assert.That(fetched.Name, Is.EqualTo("Новое название"));
        Assert.That(fetched.Price, Is.EqualTo(1_500m));
    }

    [Test]
    public async Task UpdateAsync_WhenProductNotFound_ThrowsNotFoundException()
    {
        var request = new UpdateProductRequest { Name = "Название", Description = string.Empty, Price = 100m };
        await Assert.ThatAsync(() => _sut.UpdateAsync(999, request), Throws.TypeOf<NotFoundException>());
    }

    [Test]
    public async Task DeleteAsync_RemovesProductFromDatabase()
    {
        var created = await _sut.CreateAsync(new CreateProductRequest { Name = "Временный товар", Price = 500m });
        await _sut.DeleteAsync(created.Id);
        await Assert.ThatAsync(() => _sut.GetByIdAsync(created.Id), Throws.TypeOf<NotFoundException>());
    }

    [Test]
    public async Task DeleteAsync_WhenProductNotFound_ThrowsNotFoundException()
    {
        await Assert.ThatAsync(() => _sut.DeleteAsync(999), Throws.TypeOf<NotFoundException>());
    }

    [Test]
    public async Task DeleteAsync_WhenProductHasOrderItems_ThrowsDbUpdateException()
    {
        var product = await _sut.CreateAsync(new CreateProductRequest { Name = "Товар в заказе", Price = 1_000m });
        await _orders.CreateAsync(new CreateOrderRequest
        {
            Items = new List<OrderItemRequest> { new() { ProductId = product.Id, Quantity = 1 } }
        });
        // FK Restrict: нельзя удалить товар, на который ссылаются позиции заказа
        await Assert.ThatAsync(() => _sut.DeleteAsync(product.Id), Throws.TypeOf<DbUpdateException>());
    }

    /// <summary>
    /// Создаёт несколько товаров, читает через GetAll и GetById — проверяет согласованность данных.
    /// </summary>
    [Test]
    public async Task CreateMultiple_GetAll_GetByIdEach_ReturnsConsistentData()
    {
        var a = await _sut.CreateAsync(new CreateProductRequest { Name = "Товар А", Price = 100m });
        var b = await _sut.CreateAsync(new CreateProductRequest { Name = "Товар Б", Price = 200m });
        var c = await _sut.CreateAsync(new CreateProductRequest { Name = "Товар В", Price = 300m });
        var all = await _sut.GetAllAsync();
        Assert.That(all.Count, Is.EqualTo(3));
        Assert.That((await _sut.GetByIdAsync(a.Id)).Name, Is.EqualTo("Товар А"));
        Assert.That((await _sut.GetByIdAsync(b.Id)).Name, Is.EqualTo("Товар Б"));
        Assert.That((await _sut.GetByIdAsync(c.Id)).Name, Is.EqualTo("Товар В"));
        for (var i = 0; i < 4; i++)
        {
            var extra = await _sut.CreateAsync(new CreateProductRequest { Name = $"Доп {i}", Price = 500m + i * 50m });
            await _sut.GetByIdAsync(extra.Id);
        }
        await _sut.GetAllAsync();
    }

    /// <summary>
    /// Создаёт товар, обновляет поля, проверяет персистентность, удаляет — полный цикл записи.
    /// </summary>
    [Test]
    public async Task CreateUpdateDelete_VerifyEachStep_AllPersist()
    {
        var created = await _sut.CreateAsync(new CreateProductRequest { Name = "Монитор", Price = 20_000m });
        var updated = await _sut.UpdateAsync(created.Id, new UpdateProductRequest { Name = "Монитор 4K", Description = "UHD", Price = 25_000m });
        Assert.That(updated.Name, Is.EqualTo("Монитор 4K"));
        Assert.That(updated.Price, Is.EqualTo(25_000m));
        var fetched = await _sut.GetByIdAsync(created.Id);
        Assert.That(fetched.Name, Is.EqualTo("Монитор 4K"));
        await _sut.DeleteAsync(created.Id);
        await Assert.ThatAsync(() => _sut.GetByIdAsync(created.Id), Throws.TypeOf<NotFoundException>());
        for (var i = 0; i < 4; i++)
        {
            var extra = await _sut.CreateAsync(new CreateProductRequest { Name = $"Доп {i}", Price = 1_000m + i * 100m });
            await _sut.UpdateAsync(extra.Id, new UpdateProductRequest { Name = $"Доп {i} v2", Price = 1_100m + i * 100m });
            await _sut.GetByIdAsync(extra.Id);
        }
        await _sut.GetAllAsync();
    }
}
