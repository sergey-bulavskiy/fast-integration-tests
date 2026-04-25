namespace FastIntegrationTests.Tests.IntegreSQL.Products;

/// <summary>
/// Тесты сервисного уровня: GetAll, GetById, Create для ProductService.
/// Каждый тест получает изолированный клон БД через IntegreSQL (~5 мс).
/// </summary>
public class ProductServiceCrTests : AppServiceTestBase
{
    private IProductService Sut = null!;

    /// <inheritdoc/>
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        Sut = new ProductService(new ProductRepository(Context));
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetAllAsync_WhenNoProducts_ReturnsEmptyList(int _)
    {
        var result = await Sut.GetAllAsync();

        Assert.Empty(result);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetAllAsync_WhenProductsExist_ReturnsAllProducts(int _)
    {
        await Sut.CreateAsync(new CreateProductRequest { Name = "Товар 1", Description = "Описание 1", Price = 100m });
        await Sut.CreateAsync(new CreateProductRequest { Name = "Товар 2", Description = "Описание 2", Price = 200m });

        var result = await Sut.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetByIdAsync_WhenProductExists_ReturnsProduct(int _)
    {
        var created = await Sut.CreateAsync(new CreateProductRequest { Name = "Ноутбук", Description = "Core i9", Price = 50_000m });

        var result = await Sut.GetByIdAsync(created.Id);

        Assert.Equal(created.Id, result.Id);
        Assert.Equal("Ноутбук", result.Name);
        Assert.Equal("Core i9", result.Description);
        Assert.Equal(50_000m, result.Price);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetByIdAsync_WhenProductNotFound_ThrowsNotFoundException(int _)
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Sut.GetByIdAsync(999));
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task CreateAsync_PersistsProductAndReturnsWithAssignedId(int _)
    {
        var request = new CreateProductRequest { Name = "Мышь", Description = "Беспроводная", Price = 2_500m };

        var result = await Sut.CreateAsync(request);

        Assert.True(result.Id > 0);
        Assert.Equal("Мышь", result.Name);
        Assert.Equal("Беспроводная", result.Description);
        Assert.Equal(2_500m, result.Price);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task CreateAsync_SetsCreatedAtAutomatically(int _)
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var result = await Sut.CreateAsync(new CreateProductRequest { Name = "Клавиатура", Price = 3_000m });

        var after = DateTime.UtcNow.AddSeconds(1);
        Assert.InRange(result.CreatedAt, before, after);
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
