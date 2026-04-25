namespace FastIntegrationTests.Tests.Respawn.Products;

/// <summary>
/// Тесты сервисного уровня: GetAll, GetById, Create для ProductService.
/// Каждый тест сбрасывает данные через Respawn (~1 мс), схема сохраняется.
/// </summary>
public class ProductServiceCrRespawnTests : RespawnServiceTestBase
{
    /// <summary>Создаёт новый экземпляр <see cref="ProductServiceCrRespawnTests"/>.</summary>
    /// <param name="fixture">Фикстура с контейнером и Respawner.</param>
    public ProductServiceCrRespawnTests(RespawnFixture fixture) : base(fixture) { }

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
}
