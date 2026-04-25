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

    /// <summary>
    /// Создаёт несколько категорий, проверяет GetAll и GetById каждой.
    /// </summary>
    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task CreateMultiple_GetAll_GetByIdEach_ReturnsConsistentData(int _)
    {
        var a = await Sut.CreateAsync(new CreateCategoryRequest { Name = "Электроника", Description = "Гаджеты" });
        var b = await Sut.CreateAsync(new CreateCategoryRequest { Name = "Одежда" });
        var c = await Sut.CreateAsync(new CreateCategoryRequest { Name = "Книги", Description = "Всё о книгах" });

        var all = await Sut.GetAllAsync();
        Assert.Equal(3, all.Count);
        Assert.Equal("Электроника", (await Sut.GetByIdAsync(a.Id)).Name);
        Assert.Equal("Одежда", (await Sut.GetByIdAsync(b.Id)).Name);
        Assert.Equal("Книги", (await Sut.GetByIdAsync(c.Id)).Name);

        // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
        for (var i = 0; i < 4; i++)
        {
            var extra = await Sut.CreateAsync(new CreateCategoryRequest { Name = $"Категория {i}" });
            await Sut.GetByIdAsync(extra.Id);
        }
        await Sut.GetAllAsync();
    }

    /// <summary>
    /// Создаёт категорию, обновляет, проверяет персистентность, удаляет.
    /// </summary>
    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task CreateUpdateDelete_VerifyEachStep_AllPersist(int _)
    {
        var created = await Sut.CreateAsync(new CreateCategoryRequest { Name = "Спорт", Description = "Инвентарь" });
        var updated = await Sut.UpdateAsync(created.Id, new UpdateCategoryRequest { Name = "Спорт и фитнес", Description = "Тренажёры и инвентарь" });
        Assert.Equal("Спорт и фитнес", updated.Name);

        var fetched = await Sut.GetByIdAsync(created.Id);
        Assert.Equal("Спорт и фитнес", fetched.Name);
        Assert.Equal("Тренажёры и инвентарь", fetched.Description);

        await Sut.DeleteAsync(created.Id);
        await Assert.ThrowsAsync<NotFoundException>(() => Sut.GetByIdAsync(created.Id));

        // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
        for (var i = 0; i < 4; i++)
        {
            var extra = await Sut.CreateAsync(new CreateCategoryRequest { Name = $"Доп кат {i}" });
            await Sut.UpdateAsync(extra.Id, new UpdateCategoryRequest { Name = $"Доп кат {i} v2" });
            await Sut.GetByIdAsync(extra.Id);
        }
        await Sut.GetAllAsync();
    }
}
