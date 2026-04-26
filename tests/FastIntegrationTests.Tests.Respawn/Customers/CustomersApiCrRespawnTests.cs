namespace FastIntegrationTests.Tests.Respawn.Customers;

/// <summary>
/// Тесты HTTP-уровня: GetAll, GetById для CustomersController.
/// Каждый тест сбрасывает данные через Respawn (~1 мс), схема и TestServer сохраняются.
/// </summary>
public class CustomersApiCrRespawnTests : RespawnApiTestBase
{
    /// <summary>Создаёт новый экземпляр <see cref="CustomersApiCrRespawnTests"/>.</summary>
    /// <param name="fixture">Фикстура с контейнером и Respawner.</param>
    public CustomersApiCrRespawnTests(RespawnApiFixture fixture) : base(fixture) { }

    [Fact]
    public async Task GetAll_WhenEmpty_Returns200WithEmptyArray()
    {
        var response = await Client.GetAsync("/api/customers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<List<CustomerDto>>();
        Assert.Empty(items!);
    }

    [Fact]
    public async Task GetAll_WhenExist_Returns200WithCustomers()
    {
        await CreateCustomerAsync("Иван", "ivan@example.com");
        await CreateCustomerAsync("Мария", "maria@example.com");

        var response = await Client.GetAsync("/api/customers");
        var items = await response.Content.ReadFromJsonAsync<List<CustomerDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, items!.Count);
    }

    [Fact]
    public async Task GetById_WhenExists_Returns200WithCustomer()
    {
        var created = await CreateCustomerAsync("Пётр", "petr@example.com");

        var response = await Client.GetAsync($"/api/customers/{created.Id}");
        var item = await response.Content.ReadFromJsonAsync<CustomerDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(created.Id, item!.Id);
        Assert.Equal("Пётр", item.Name);
    }

    [Fact]
    public async Task GetById_WhenNotFound_Returns404()
    {
        var response = await Client.GetAsync($"/api/customers/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Создаёт несколько покупателей через API, проверяет GetAll и GetById каждого.
    /// </summary>
    [Fact]
    public async Task CreateMultiple_GetAll_GetByIdEach_ReturnsConsistentData()
    {
        var a = await CreateCustomerAsync("Иван", "ivan@example.com");
        var b = await CreateCustomerAsync("Мария", "maria@example.com");
        var c = await CreateCustomerAsync("Пётр", "peter@example.com");

        var all = await Client.GetAsync("/api/customers");
        var list = await all.Content.ReadFromJsonAsync<List<CustomerDto>>();
        Assert.Equal(3, list!.Count);

        var fa = await (await Client.GetAsync($"/api/customers/{a.Id}")).Content.ReadFromJsonAsync<CustomerDto>();
        Assert.Equal("Иван", fa!.Name);
        Assert.Equal(CustomerStatus.Active, fa.Status);

        // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
        for (var i = 0; i < 4; i++)
        {
            var extra = await CreateCustomerAsync($"Доп {i}", $"extra{i}@example.com");
            await Client.GetAsync($"/api/customers/{extra.Id}");
        }
        await Client.GetAsync("/api/customers");
    }

    /// <summary>
    /// Создаёт покупателя, выполняет ban → activate → deactivate через API.
    /// </summary>
    [Fact]
    public async Task CreateBanActivateDeactivate_StatusTransitionsCorrect()
    {
        var created = await CreateCustomerAsync("Клиент", "client@example.com");

        Assert.Equal(HttpStatusCode.NoContent, (await Client.PostAsync($"/api/customers/{created.Id}/ban", null)).StatusCode);
        var banned = await (await Client.GetAsync($"/api/customers/{created.Id}")).Content.ReadFromJsonAsync<CustomerDto>();
        Assert.Equal(CustomerStatus.Banned, banned!.Status);

        Assert.Equal(HttpStatusCode.NoContent, (await Client.PostAsync($"/api/customers/{created.Id}/activate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Client.PostAsync($"/api/customers/{created.Id}/deactivate", null)).StatusCode);

        var fetched = await (await Client.GetAsync($"/api/customers/{created.Id}")).Content.ReadFromJsonAsync<CustomerDto>();
        Assert.Equal(CustomerStatus.Inactive, fetched!.Status);

        // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
        for (var i = 0; i < 3; i++)
        {
            var extra = await CreateCustomerAsync($"Доп {i}", $"pad{i}@example.com");
            await Client.PostAsync($"/api/customers/{extra.Id}/ban", null);
            await Client.GetAsync($"/api/customers/{extra.Id}");
        }
        await Client.GetAsync("/api/customers");
    }

    // --- helpers ---

    /// <summary>
    /// Создаёт покупателя через API и возвращает его DTO.
    /// </summary>
    /// <param name="name">Имя покупателя.</param>
    /// <param name="email">Email покупателя.</param>
    /// <param name="ct">Токен отмены операции.</param>
    private async Task<CustomerDto> CreateCustomerAsync(string name, string email, CancellationToken ct = default)
    {
        var response = await Client.PostAsJsonAsync("/api/customers",
            new CreateCustomerRequest { Name = name, Email = email }, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CustomerDto>(ct))!;
    }
}
