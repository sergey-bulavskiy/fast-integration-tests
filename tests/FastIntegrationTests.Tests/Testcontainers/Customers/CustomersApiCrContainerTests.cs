namespace FastIntegrationTests.Tests.Testcontainers.Customers;

/// <summary>
/// Тесты HTTP-уровня: GetAll, GetById для CustomersController.
/// Каждый тест создаёт изолированную БД в реальном контейнере PostgreSQL и отдельный TestServer.
/// </summary>
public class CustomersApiCrContainerTests : ContainerApiTestBase
{
    /// <summary>
    /// Создаёт новый экземпляр <see cref="CustomersApiCrContainerTests"/>.
    /// </summary>
    /// <param name="fixture">Запущенный контейнер с СУБД.</param>
    public CustomersApiCrContainerTests(ContainerFixture fixture) : base(fixture) { }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetAll_WhenEmpty_Returns200WithEmptyArray(int _)
    {
        var response = await Client.GetAsync("/api/customers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<List<CustomerDto>>();
        Assert.Empty(items!);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetAll_WhenExist_Returns200WithCustomers(int _)
    {
        await CreateCustomerAsync("Иван", "ivan@example.com");
        await CreateCustomerAsync("Мария", "maria@example.com");

        var response = await Client.GetAsync("/api/customers");
        var items = await response.Content.ReadFromJsonAsync<List<CustomerDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, items!.Count);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetById_WhenExists_Returns200WithCustomer(int _)
    {
        var created = await CreateCustomerAsync("Пётр", "petr@example.com");

        var response = await Client.GetAsync($"/api/customers/{created.Id}");
        var item = await response.Content.ReadFromJsonAsync<CustomerDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(created.Id, item!.Id);
        Assert.Equal("Пётр", item.Name);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetById_WhenNotFound_Returns404(int _)
    {
        var response = await Client.GetAsync($"/api/customers/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
