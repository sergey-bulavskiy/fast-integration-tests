namespace FastIntegrationTests.Tests.IntegreSQL.Suppliers;

/// <summary>
/// Тесты HTTP-уровня: Create, Update, Delete, Activate, Deactivate для SuppliersController.
/// Каждый тест получает клон шаблонной БД IntegreSQL (~5 мс) и отдельный TestServer.
/// </summary>
public class SuppliersApiUdTests : ComponentTestBase
{
    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task Create_ValidRequest_Returns201WithLocationHeader(int _)
    {
        var request = new CreateSupplierRequest { Name = "ООО Альфа", ContactEmail = "alpha@vendor.com", Country = "Россия" };

        var response = await Client.PostAsJsonAsync("/api/suppliers", request);
        var item = await response.Content.ReadFromJsonAsync<SupplierDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.NotEqual(Guid.Empty, item!.Id);
        Assert.Equal("ООО Альфа", item.Name);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task Create_WhenDuplicateEmail_Returns409(int _)
    {
        await Client.PostAsJsonAsync("/api/suppliers", new CreateSupplierRequest { Name = "ООО Альфа", ContactEmail = "dup@vendor.com", Country = "Россия" });

        var response = await Client.PostAsJsonAsync("/api/suppliers", new CreateSupplierRequest { Name = "Другой", ContactEmail = "dup@vendor.com", Country = "Беларусь" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task Update_WhenExists_Returns200WithUpdatedFields(int _)
    {
        var created = await CreateSupplierAsync("Старый", "old@vendor.com");

        var response = await Client.PutAsJsonAsync($"/api/suppliers/{created.Id}",
            new UpdateSupplierRequest { Name = "Новый", ContactEmail = "new@vendor.com", Country = "Казахстан" });
        var updated = await response.Content.ReadFromJsonAsync<SupplierDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Новый", updated!.Name);
        Assert.Equal("new@vendor.com", updated.ContactEmail);
        Assert.Equal("Казахстан", updated.Country);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task Update_WhenNotFound_Returns404(int _)
    {
        var response = await Client.PutAsJsonAsync($"/api/suppliers/{Guid.NewGuid()}",
            new UpdateSupplierRequest { Name = "Любой", ContactEmail = "any@vendor.com", Country = "Россия" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task Delete_WhenExists_Returns204(int _)
    {
        var created = await CreateSupplierAsync("Удаляемый", "del@vendor.com");

        var response = await Client.DeleteAsync($"/api/suppliers/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task Activate_WhenExists_Returns204(int _)
    {
        var created = await CreateSupplierAsync("Неактивный", "inactive@vendor.com");
        await Client.PostAsync($"/api/suppliers/{created.Id}/deactivate", null);

        var response = await Client.PostAsync($"/api/suppliers/{created.Id}/activate", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task Deactivate_WhenExists_Returns204(int _)
    {
        var created = await CreateSupplierAsync("Активный", "active@vendor.com");

        var response = await Client.PostAsync($"/api/suppliers/{created.Id}/deactivate", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // --- helpers ---

    /// <summary>
    /// Создаёт поставщика через API и возвращает его DTO.
    /// </summary>
    /// <param name="name">Название поставщика.</param>
    /// <param name="email">Контактный email.</param>
    /// <param name="ct">Токен отмены операции.</param>
    private async Task<SupplierDto> CreateSupplierAsync(string name, string email, CancellationToken ct = default)
    {
        var response = await Client.PostAsJsonAsync("/api/suppliers",
            new CreateSupplierRequest { Name = name, ContactEmail = email, Country = "Россия" }, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SupplierDto>(ct))!;
    }
}
