namespace FastIntegrationTests.Tests.Testcontainers.Categories;

/// <summary>
/// Тесты сервисного уровня: GetAll, GetById, Create для CategoryService.
/// Каждый тест создаёт изолированную БД в реальном контейнере PostgreSQL.
/// </summary>
public class CategoryServiceCrContainerTests : IAsyncLifetime, IClassFixture<ContainerFixture>
{
    private readonly ContainerFixture _fixture;
    private ShopDbContext _context = null!;
    private ICategoryService Sut = null!;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="CategoryServiceCrContainerTests"/>.
    /// </summary>
    /// <param name="fixture">Запущенный контейнер с СУБД.</param>
    public CategoryServiceCrContainerTests(ContainerFixture fixture) => _fixture = fixture;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        _context = await new TestDbFactory(_fixture).CreateAsync();
        Sut = new CategoryService(new CategoryRepository(_context));
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetAllAsync_WhenNoCategories_ReturnsEmptyList(int _)
    {
        var result = await Sut.GetAllAsync();

        Assert.Empty(result);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetAllAsync_WhenCategoriesExist_ReturnsAllCategories(int _)
    {
        await Sut.CreateAsync(new CreateCategoryRequest { Name = "Электроника", Description = "Гаджеты" });
        await Sut.CreateAsync(new CreateCategoryRequest { Name = "Одежда" });

        var result = await Sut.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetByIdAsync_WhenExists_ReturnsCategory(int _)
    {
        var created = await Sut.CreateAsync(new CreateCategoryRequest { Name = "Книги", Description = "Художественная литература" });

        var result = await Sut.GetByIdAsync(created.Id);

        Assert.Equal(created.Id, result.Id);
        Assert.Equal("Книги", result.Name);
        Assert.Equal("Художественная литература", result.Description);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetByIdAsync_WhenNotFound_ThrowsNotFoundException(int _)
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Sut.GetByIdAsync(Guid.NewGuid()));
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task CreateAsync_PersistsAndReturns(int _)
    {
        var result = await Sut.CreateAsync(new CreateCategoryRequest { Name = "Спорт", Description = "Инвентарь" });

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Спорт", result.Name);
        Assert.Equal("Инвентарь", result.Description);
        Assert.True(result.CreatedAt > DateTime.UtcNow.AddSeconds(-5));
    }
}
