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
}
