namespace FastIntegrationTests.Tests.Respawn.Products;

/// <summary>
/// Тесты сервисного уровня: Update, Delete для ProductService.
/// Каждый тест сбрасывает данные через Respawn (~1 мс), схема сохраняется.
/// </summary>
public class ProductServiceUdRespawnTests : RespawnServiceTestBase
{
    /// <summary>Создаёт новый экземпляр <see cref="ProductServiceUdRespawnTests"/>.</summary>
    /// <param name="fixture">Фикстура с контейнером и Respawner.</param>
    public ProductServiceUdRespawnTests(RespawnFixture fixture) : base(fixture) { }

    private IProductService Sut = null!;
    private IOrderService _orders = null!;

    /// <inheritdoc/>
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        var productRepo = new ProductRepository(Context);
        var orderRepo = new OrderRepository(Context);
        Sut = new ProductService(productRepo);
        _orders = new OrderService(orderRepo, productRepo);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task UpdateAsync_UpdatesProductFieldsInDatabase(int _)
    {
        var created = await Sut.CreateAsync(new CreateProductRequest { Name = "Старое название", Price = 1_000m });
        var updateRequest = new UpdateProductRequest { Name = "Новое название", Description = "Новое описание", Price = 1_500m };

        var updated = await Sut.UpdateAsync(created.Id, updateRequest);

        Assert.Equal("Новое название", updated.Name);
        Assert.Equal("Новое описание", updated.Description);
        Assert.Equal(1_500m, updated.Price);

        var fetched = await Sut.GetByIdAsync(created.Id);
        Assert.Equal("Новое название", fetched.Name);
        Assert.Equal(1_500m, fetched.Price);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task UpdateAsync_WhenProductNotFound_ThrowsNotFoundException(int _)
    {
        var request = new UpdateProductRequest { Name = "Название", Description = string.Empty, Price = 100m };

        await Assert.ThrowsAsync<NotFoundException>(() => Sut.UpdateAsync(999, request));
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task DeleteAsync_RemovesProductFromDatabase(int _)
    {
        var created = await Sut.CreateAsync(new CreateProductRequest { Name = "Временный товар", Price = 500m });

        await Sut.DeleteAsync(created.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => Sut.GetByIdAsync(created.Id));
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task DeleteAsync_WhenProductNotFound_ThrowsNotFoundException(int _)
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Sut.DeleteAsync(999));
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task DeleteAsync_WhenProductHasOrderItems_ThrowsDbUpdateException(int _)
    {
        var product = await Sut.CreateAsync(new CreateProductRequest { Name = "Товар в заказе", Price = 1_000m });
        await _orders.CreateAsync(new CreateOrderRequest
        {
            Items = new List<OrderItemRequest> { new() { ProductId = product.Id, Quantity = 1 } }
        });

        // FK Restrict: нельзя удалить товар, на который ссылаются позиции заказа
        await Assert.ThrowsAsync<DbUpdateException>(() => Sut.DeleteAsync(product.Id));
    }

    /// <summary>
    /// Создаёт несколько товаров, читает через GetAll и GetById — проверяет согласованность данных.
    /// </summary>
    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task CreateMultiple_GetAll_GetByIdEach_ReturnsConsistentData(int _)
    {
        var a = await Sut.CreateAsync(new CreateProductRequest { Name = "Товар А", Price = 100m });
        var b = await Sut.CreateAsync(new CreateProductRequest { Name = "Товар Б", Price = 200m });
        var c = await Sut.CreateAsync(new CreateProductRequest { Name = "Товар В", Price = 300m });

        var all = await Sut.GetAllAsync();
        Assert.Equal(3, all.Count);
        Assert.Equal("Товар А", (await Sut.GetByIdAsync(a.Id)).Name);
        Assert.Equal("Товар Б", (await Sut.GetByIdAsync(b.Id)).Name);
        Assert.Equal("Товар В", (await Sut.GetByIdAsync(c.Id)).Name);

        // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
        for (var i = 0; i < 4; i++)
        {
            var extra = await Sut.CreateAsync(new CreateProductRequest { Name = $"Доп {i}", Price = 500m + i * 50m });
            await Sut.GetByIdAsync(extra.Id);
        }
        await Sut.GetAllAsync();
    }

    /// <summary>
    /// Создаёт товар, обновляет поля, проверяет персистентность, удаляет — полный цикл записи.
    /// </summary>
    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task CreateUpdateDelete_VerifyEachStep_AllPersist(int _)
    {
        var created = await Sut.CreateAsync(new CreateProductRequest { Name = "Монитор", Price = 20_000m });
        var updated = await Sut.UpdateAsync(created.Id, new UpdateProductRequest { Name = "Монитор 4K", Description = "UHD", Price = 25_000m });
        Assert.Equal("Монитор 4K", updated.Name);
        Assert.Equal(25_000m, updated.Price);

        var fetched = await Sut.GetByIdAsync(created.Id);
        Assert.Equal("Монитор 4K", fetched.Name);

        await Sut.DeleteAsync(created.Id);
        await Assert.ThrowsAsync<NotFoundException>(() => Sut.GetByIdAsync(created.Id));

        // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
        for (var i = 0; i < 4; i++)
        {
            var extra = await Sut.CreateAsync(new CreateProductRequest { Name = $"Доп {i}", Price = 1_000m + i * 100m });
            await Sut.UpdateAsync(extra.Id, new UpdateProductRequest { Name = $"Доп {i} v2", Price = 1_100m + i * 100m });
            await Sut.GetByIdAsync(extra.Id);
        }
        await Sut.GetAllAsync();
    }
}
