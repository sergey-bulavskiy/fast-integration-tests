namespace FastIntegrationTests.Tests.Respawn.Suppliers;

/// <summary>
/// Тесты HTTP-уровня: GetAll, GetById для SuppliersController.
/// Каждый тест сбрасывает данные через Respawn (~1 мс), схема и TestServer сохраняются.
/// </summary>
public class SuppliersApiCrRespawnTests : RespawnApiTestBase
{
    /// <summary>Создаёт новый экземпляр <see cref="SuppliersApiCrRespawnTests"/>.</summary>
    /// <param name="fixture">Фикстура с контейнером и Respawner.</param>
    public SuppliersApiCrRespawnTests(RespawnApiFixture fixture) : base(fixture) { }

    [Fact]
    public async Task GetAll_WhenEmpty_Returns200WithEmptyArray()
    {
        var response = await Client.GetAsync("/api/suppliers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<List<SupplierDto>>();
        Assert.Empty(items!);
    }

    [Fact]
    public async Task GetAll_WhenExist_Returns200WithSuppliers()
    {
        await CreateSupplierAsync("ООО Альфа", "alpha@vendor.com");
        await CreateSupplierAsync("ООО Бета", "beta@vendor.com");

        var response = await Client.GetAsync("/api/suppliers");
        var items = await response.Content.ReadFromJsonAsync<List<SupplierDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, items!.Count);
    }

    [Fact]
    public async Task GetById_WhenExists_Returns200WithSupplier()
    {
        var created = await CreateSupplierAsync("ООО Гамма", "gamma@vendor.com");

        var response = await Client.GetAsync($"/api/suppliers/{created.Id}");
        var item = await response.Content.ReadFromJsonAsync<SupplierDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(created.Id, item!.Id);
        Assert.Equal("ООО Гамма", item.Name);
    }

    [Fact]
    public async Task GetById_WhenNotFound_Returns404()
    {
        var response = await Client.GetAsync($"/api/suppliers/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Создаёт несколько поставщиков через API, проверяет GetAll и GetById каждого.
    /// </summary>
    [Fact]
    public async Task CreateMultiple_GetAll_GetByIdEach_ReturnsConsistentData()
    {
        var a = await CreateSupplierAsync("ООО Альфа", "alpha@example.com");
        var b = await CreateSupplierAsync("ИП Бета", "beta@example.com");
        var c = await CreateSupplierAsync("ЗАО Гамма", "gamma@example.com");

        var all = await Client.GetAsync("/api/suppliers");
        var list = await all.Content.ReadFromJsonAsync<List<SupplierDto>>();
        Assert.Equal(3, list!.Count);

        var fa = await (await Client.GetAsync($"/api/suppliers/{a.Id}")).Content.ReadFromJsonAsync<SupplierDto>();
        Assert.Equal("ООО Альфа", fa!.Name);

        // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
        for (var i = 0; i < 4; i++)
        {
            var extra = await CreateSupplierAsync($"Доп {i}", $"extra{i}@example.com");
            await Client.GetAsync($"/api/suppliers/{extra.Id}");
        }
        await Client.GetAsync("/api/suppliers");
    }

    /// <summary>
    /// Создаёт поставщика, обновляет, деактивирует, активирует через API.
    /// </summary>
    [Fact]
    public async Task CreateUpdateDeactivateActivate_AllPersist()
    {
        var created = await CreateSupplierAsync("ООО Старт", "start@example.com");

        var putResp = await Client.PutAsJsonAsync($"/api/suppliers/{created.Id}",
            new UpdateSupplierRequest { Name = "ООО Финиш", ContactEmail = "start@example.com", Country = "Беларусь" });
        Assert.Equal(HttpStatusCode.OK, putResp.StatusCode);

        var deact = await Client.PostAsync($"/api/suppliers/{created.Id}/deactivate", null);
        Assert.Equal(HttpStatusCode.NoContent, deact.StatusCode);

        var act = await Client.PostAsync($"/api/suppliers/{created.Id}/activate", null);
        Assert.Equal(HttpStatusCode.NoContent, act.StatusCode);

        var fetched = await (await Client.GetAsync($"/api/suppliers/{created.Id}")).Content.ReadFromJsonAsync<SupplierDto>();
        Assert.Equal("ООО Финиш", fetched!.Name);
        Assert.True(fetched.IsActive);

        // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
        for (var i = 0; i < 3; i++)
        {
            var extra = await CreateSupplierAsync($"Доп {i}", $"pad{i}@example.com");
            await Client.PostAsync($"/api/suppliers/{extra.Id}/deactivate", null);
            await Client.GetAsync($"/api/suppliers/{extra.Id}");
        }
        await Client.GetAsync("/api/suppliers");
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
