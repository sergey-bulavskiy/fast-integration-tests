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
