namespace FastIntegrationTests.Tests.IntegreSQL.Categories;

/// <summary>
/// Тесты HTTP-уровня: Create, Update, Delete для CategoriesController.
/// Каждый тест получает изолированный клон БД через IntegreSQL (~5 мс) и отдельный TestServer.
/// </summary>
public class CategoriesApiUdTests : ComponentTestBase
{
    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task Create_ValidRequest_Returns201WithLocationHeader(int _)
    {
        var request = new CreateCategoryRequest { Name = "Электроника", Description = "Гаджеты" };

        var response = await Client.PostAsJsonAsync("/api/categories", request);
        var item = await response.Content.ReadFromJsonAsync<CategoryDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.NotEqual(Guid.Empty, item!.Id);
        Assert.Equal("Электроника", item.Name);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task Create_WhenDuplicateName_Returns409(int _)
    {
        await Client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest { Name = "Дубль" });

        var response = await Client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest { Name = "Дубль" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task Update_WhenExists_Returns200WithUpdatedFields(int _)
    {
        var created = await CreateCategoryAsync("Старая");

        var response = await Client.PutAsJsonAsync($"/api/categories/{created.Id}",
            new UpdateCategoryRequest { Name = "Новая", Description = "Обновлено" });
        var updated = await response.Content.ReadFromJsonAsync<CategoryDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Новая", updated!.Name);
        Assert.Equal("Обновлено", updated.Description);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task Update_WhenNotFound_Returns404(int _)
    {
        var response = await Client.PutAsJsonAsync($"/api/categories/{Guid.NewGuid()}",
            new UpdateCategoryRequest { Name = "Любая" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task Delete_WhenExists_Returns204(int _)
    {
        var created = await CreateCategoryAsync("Удаляемая");

        var response = await Client.DeleteAsync($"/api/categories/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
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
