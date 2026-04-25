namespace FastIntegrationTests.Tests.IntegreSQL.Customers;

/// <summary>
/// Тесты сервисного уровня: GetAll, GetById, Create для CustomerService.
/// Каждый тест получает клон шаблонной БД IntegreSQL (~5 мс).
/// </summary>
public class CustomerServiceCrTests : AppServiceTestBase
{
    private ICustomerService Sut = null!;

    /// <inheritdoc/>
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        Sut = new CustomerService(new CustomerRepository(Context));
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetAllAsync_WhenNoCustomers_ReturnsEmptyList(int _)
    {
        var result = await Sut.GetAllAsync();

        Assert.Empty(result);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetAllAsync_WhenCustomersExist_ReturnsAllCustomers(int _)
    {
        await Sut.CreateAsync(new CreateCustomerRequest { Name = "Иван", Email = "ivan@example.com" });
        await Sut.CreateAsync(new CreateCustomerRequest { Name = "Мария", Email = "maria@example.com" });

        var result = await Sut.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetByIdAsync_WhenExists_ReturnsCustomer(int _)
    {
        var created = await Sut.CreateAsync(new CreateCustomerRequest { Name = "Пётр", Email = "petr@example.com", Phone = "+79001234567" });

        var result = await Sut.GetByIdAsync(created.Id);

        Assert.Equal(created.Id, result.Id);
        Assert.Equal("Пётр", result.Name);
        Assert.Equal("petr@example.com", result.Email);
        Assert.Equal("+79001234567", result.Phone);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetByIdAsync_WhenNotFound_ThrowsNotFoundException(int _)
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Sut.GetByIdAsync(Guid.NewGuid()));
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task CreateAsync_PersistsAndReturns(int _)
    {
        var result = await Sut.CreateAsync(new CreateCustomerRequest { Name = "Анна", Email = "anna@example.com" });

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Анна", result.Name);
        Assert.Equal("anna@example.com", result.Email);
        Assert.Equal(CustomerStatus.Active, result.Status);
        Assert.True(result.CreatedAt > DateTime.UtcNow.AddSeconds(-5));
    }
}
