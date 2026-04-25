namespace FastIntegrationTests.Tests.IntegreSQL.Suppliers;

/// <summary>
/// Тесты HTTP-уровня: GetAll, GetById для SuppliersController.
/// Каждый тест получает клон шаблонной БД IntegreSQL (~5 мс) и отдельный TestServer.
/// </summary>
public class SuppliersApiCrTests : ComponentTestBase
{
    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetAll_WhenEmpty_Returns200WithEmptyArray(int _)
    {
        var response = await Client.GetAsync("/api/suppliers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<List<SupplierDto>>();
        Assert.Empty(items!);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetAll_WhenExist_Returns200WithSuppliers(int _)
    {
        await CreateSupplierAsync("ООО Альфа", "alpha@vendor.com");
        await CreateSupplierAsync("ООО Бета", "beta@vendor.com");

        var response = await Client.GetAsync("/api/suppliers");
        var items = await response.Content.ReadFromJsonAsync<List<SupplierDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, items!.Count);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetById_WhenExists_Returns200WithSupplier(int _)
    {
        var created = await CreateSupplierAsync("ООО Гамма", "gamma@vendor.com");

        var response = await Client.GetAsync($"/api/suppliers/{created.Id}");
        var item = await response.Content.ReadFromJsonAsync<SupplierDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(created.Id, item!.Id);
        Assert.Equal("ООО Гамма", item.Name);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetById_WhenNotFound_Returns404(int _)
    {
        var response = await Client.GetAsync($"/api/suppliers/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
