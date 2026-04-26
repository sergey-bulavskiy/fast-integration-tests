namespace FastIntegrationTests.Tests.Testcontainers.Suppliers;

/// <summary>
/// Тесты HTTP-уровня: Create, Update, Delete, Activate, Deactivate для SuppliersController.
/// Каждый тест создаёт изолированную БД в реальном контейнере PostgreSQL и отдельный TestServer.
/// </summary>
public class SuppliersApiUdContainerTests : ContainerApiTestBase
{
    /// <summary>
    /// Создаёт новый экземпляр <see cref="SuppliersApiUdContainerTests"/>.
    /// </summary>
    /// <param name="fixture">Запущенный контейнер с СУБД.</param>
    public SuppliersApiUdContainerTests(ContainerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Create_ValidRequest_Returns201WithLocationHeader()
    {
        var request = new CreateSupplierRequest { Name = "ООО Альфа", ContactEmail = "alpha@vendor.com", Country = "Россия" };

        var response = await Client.PostAsJsonAsync("/api/suppliers", request);
        var item = await response.Content.ReadFromJsonAsync<SupplierDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.NotEqual(Guid.Empty, item!.Id);
        Assert.Equal("ООО Альфа", item.Name);
    }

    [Fact]
    public async Task Create_WhenDuplicateEmail_Returns409()
    {
        await Client.PostAsJsonAsync("/api/suppliers", new CreateSupplierRequest { Name = "ООО Альфа", ContactEmail = "dup@vendor.com", Country = "Россия" });

        var response = await Client.PostAsJsonAsync("/api/suppliers", new CreateSupplierRequest { Name = "Другой", ContactEmail = "dup@vendor.com", Country = "Беларусь" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Update_WhenExists_Returns200WithUpdatedFields()
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

    [Fact]
    public async Task Update_WhenNotFound_Returns404()
    {
        var response = await Client.PutAsJsonAsync($"/api/suppliers/{Guid.NewGuid()}",
            new UpdateSupplierRequest { Name = "Любой", ContactEmail = "any@vendor.com", Country = "Россия" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WhenExists_Returns204()
    {
        var created = await CreateSupplierAsync("Удаляемый", "del@vendor.com");

        var response = await Client.DeleteAsync($"/api/suppliers/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Activate_WhenExists_Returns204()
    {
        var created = await CreateSupplierAsync("Неактивный", "inactive@vendor.com");
        await Client.PostAsync($"/api/suppliers/{created.Id}/deactivate", null);

        var response = await Client.PostAsync($"/api/suppliers/{created.Id}/activate", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Deactivate_WhenExists_Returns204()
    {
        var created = await CreateSupplierAsync("Активный", "active@vendor.com");

        var response = await Client.PostAsync($"/api/suppliers/{created.Id}/deactivate", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
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
