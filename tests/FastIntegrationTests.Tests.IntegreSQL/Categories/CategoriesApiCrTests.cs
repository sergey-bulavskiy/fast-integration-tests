namespace FastIntegrationTests.Tests.IntegreSQL.Categories;

/// <summary>
/// Тесты HTTP-уровня: GetAll, GetById для CategoriesController.
/// Каждый тест получает изолированный клон БД через IntegreSQL (~5 мс) и отдельный TestServer.
/// </summary>
public class CategoriesApiCrTests : ComponentTestBase
{
    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetAll_WhenEmpty_Returns200WithEmptyArray(int _)
    {
        var response = await Client.GetAsync("/api/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<List<CategoryDto>>();
        Assert.Empty(items!);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetAll_WhenExist_Returns200WithCategories(int _)
    {
        await CreateCategoryAsync("Электроника");
        await CreateCategoryAsync("Одежда");

        var response = await Client.GetAsync("/api/categories");
        var items = await response.Content.ReadFromJsonAsync<List<CategoryDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, items!.Count);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetById_WhenExists_Returns200WithCategory(int _)
    {
        var created = await CreateCategoryAsync("Книги");

        var response = await Client.GetAsync($"/api/categories/{created.Id}");
        var item = await response.Content.ReadFromJsonAsync<CategoryDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(created.Id, item!.Id);
        Assert.Equal("Книги", item.Name);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetById_WhenNotFound_Returns404(int _)
    {
        var response = await Client.GetAsync($"/api/categories/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Создаёт несколько категорий через API, проверяет GetAll и GetById каждой.
    /// </summary>
    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task CreateMultiple_GetAll_GetByIdEach_ReturnsConsistentData(int _)
    {
        var a = await CreateCategoryAsync("Электроника");
        var b = await CreateCategoryAsync("Одежда");
        var c = await CreateCategoryAsync("Книги");

        var all = await Client.GetAsync("/api/categories");
        var list = await all.Content.ReadFromJsonAsync<List<CategoryDto>>();
        Assert.Equal(3, list!.Count);

        var fa = await (await Client.GetAsync($"/api/categories/{a.Id}")).Content.ReadFromJsonAsync<CategoryDto>();
        var fb = await (await Client.GetAsync($"/api/categories/{b.Id}")).Content.ReadFromJsonAsync<CategoryDto>();
        var fc = await (await Client.GetAsync($"/api/categories/{c.Id}")).Content.ReadFromJsonAsync<CategoryDto>();
        Assert.Equal("Электроника", fa!.Name);
        Assert.Equal("Одежда", fb!.Name);
        Assert.Equal("Книги", fc!.Name);

        // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
        for (var i = 0; i < 4; i++)
        {
            var extra = await CreateCategoryAsync($"Категория {i}");
            await Client.GetAsync($"/api/categories/{extra.Id}");
        }
        await Client.GetAsync("/api/categories");
    }

    /// <summary>
    /// Создаёт категорию, обновляет через PUT, проверяет GET, удаляет — полный HTTP-цикл.
    /// </summary>
    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task CreateUpdateDelete_VerifyEachStep_AllPersist(int _)
    {
        var created = await CreateCategoryAsync("Спорт");

        var putResp = await Client.PutAsJsonAsync($"/api/categories/{created.Id}",
            new UpdateCategoryRequest { Name = "Спорт и фитнес", Description = "Обновлено" });
        Assert.Equal(HttpStatusCode.OK, putResp.StatusCode);
        var updated = await putResp.Content.ReadFromJsonAsync<CategoryDto>();
        Assert.Equal("Спорт и фитнес", updated!.Name);

        var getResp = await Client.GetAsync($"/api/categories/{created.Id}");
        var fetched = await getResp.Content.ReadFromJsonAsync<CategoryDto>();
        Assert.Equal("Спорт и фитнес", fetched!.Name);

        var delResp = await Client.DeleteAsync($"/api/categories/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delResp.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync($"/api/categories/{created.Id}")).StatusCode);

        // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
        for (var i = 0; i < 4; i++)
        {
            var extra = await CreateCategoryAsync($"Доп кат {i}");
            await Client.PutAsJsonAsync($"/api/categories/{extra.Id}",
                new UpdateCategoryRequest { Name = $"Доп кат {i} v2" });
            await Client.GetAsync($"/api/categories/{extra.Id}");
        }
        await Client.GetAsync("/api/categories");
    }

    // --- helpers ---

    /// <summary>
    /// Создаёт категорию через API и возвращает её DTO.
    /// </summary>
    /// <param name="name">Название категории.</param>
    /// <param name="ct">Токен отмены операции.</param>
    private async Task<CategoryDto> CreateCategoryAsync(string name, CancellationToken ct = default)
    {
        var response = await Client.PostAsJsonAsync("/api/categories",
            new CreateCategoryRequest { Name = name }, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CategoryDto>(ct))!;
    }
}
