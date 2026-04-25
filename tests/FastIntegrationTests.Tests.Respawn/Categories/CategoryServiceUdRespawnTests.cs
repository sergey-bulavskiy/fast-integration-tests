namespace FastIntegrationTests.Tests.Respawn.Categories;

/// <summary>
/// Тесты сервисного уровня: Create (ошибки), Update, Delete для CategoryService.
/// Каждый тест сбрасывает данные через Respawn (~1 мс), схема сохраняется.
/// </summary>
public class CategoryServiceUdRespawnTests : RespawnServiceTestBase
{
    /// <summary>Создаёт новый экземпляр <see cref="CategoryServiceUdRespawnTests"/>.</summary>
    /// <param name="fixture">Фикстура с контейнером и Respawner.</param>
    public CategoryServiceUdRespawnTests(RespawnFixture fixture) : base(fixture) { }

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
