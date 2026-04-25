namespace FastIntegrationTests.Tests.IntegreSQL.Categories;

/// <summary>
/// Тесты сервисного уровня: Create (ошибки), Update, Delete для CategoryService.
/// Каждый тест получает изолированный клон БД через IntegreSQL (~5 мс).
/// </summary>
public class CategoryServiceUdTests : AppServiceTestBase
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
    public async Task CreateAsync_WhenDuplicateName_ThrowsDuplicateValueException(int _)
    {
        await Sut.CreateAsync(new CreateCategoryRequest { Name = "Электроника" });

        await Assert.ThrowsAsync<DuplicateValueException>(
            () => Sut.CreateAsync(new CreateCategoryRequest { Name = "Электроника" }));
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task UpdateAsync_UpdatesFields(int _)
    {
        var created = await Sut.CreateAsync(new CreateCategoryRequest { Name = "Старое", Description = "Старое описание" });

        var updated = await Sut.UpdateAsync(created.Id, new UpdateCategoryRequest { Name = "Новое", Description = "Новое описание" });

        Assert.Equal("Новое", updated.Name);
        Assert.Equal("Новое описание", updated.Description);

        var fetched = await Sut.GetByIdAsync(created.Id);
        Assert.Equal("Новое", fetched.Name);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task UpdateAsync_WhenNotFound_ThrowsNotFoundException(int _)
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => Sut.UpdateAsync(Guid.NewGuid(), new UpdateCategoryRequest { Name = "Любое" }));
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task DeleteAsync_RemovesCategory(int _)
    {
        var created = await Sut.CreateAsync(new CreateCategoryRequest { Name = "Удаляемая" });

        await Sut.DeleteAsync(created.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => Sut.GetByIdAsync(created.Id));
    }
}
