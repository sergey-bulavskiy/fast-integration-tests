namespace FastIntegrationTests.Tests.IntegreSQL.Categories;

/// <summary>
/// Тесты сервисного уровня: GetAll, GetById, Create для CategoryService.
/// Каждый тест получает изолированный клон БД через IntegreSQL (~5 мс).
/// </summary>
public class CategoryServiceCrTests : AppServiceTestBase
{
    private ICategoryService Sut = null!;

    /// <inheritdoc/>
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        Sut = new CategoryService(new CategoryRepository(Context));
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
