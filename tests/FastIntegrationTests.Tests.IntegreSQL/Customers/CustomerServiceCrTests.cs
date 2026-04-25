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

    /// <summary>
    /// Создаёт несколько покупателей, проверяет GetAll и GetById каждого.
    /// </summary>
    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task CreateMultiple_GetAll_GetByIdEach_ReturnsConsistentData(int _)
    {
        var a = await Sut.CreateAsync(new CreateCustomerRequest { Name = "Иван", Email = "ivan@example.com" });
        var b = await Sut.CreateAsync(new CreateCustomerRequest { Name = "Мария", Email = "maria@example.com" });
        var c = await Sut.CreateAsync(new CreateCustomerRequest { Name = "Пётр", Email = "peter@example.com" });

        var all = await Sut.GetAllAsync();
        Assert.Equal(3, all.Count);
        Assert.Equal("Иван", (await Sut.GetByIdAsync(a.Id)).Name);
        Assert.Equal("Мария", (await Sut.GetByIdAsync(b.Id)).Name);
        Assert.Equal("Пётр", (await Sut.GetByIdAsync(c.Id)).Name);

        // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
        for (var i = 0; i < 4; i++)
        {
            var extra = await Sut.CreateAsync(new CreateCustomerRequest { Name = $"Доп {i}", Email = $"extra{i}@example.com" });
            await Sut.GetByIdAsync(extra.Id);
        }
        await Sut.GetAllAsync();
    }

    /// <summary>
    /// Создаёт покупателя, выполняет Ban → Activate → Deactivate, проверяет статус после каждого.
    /// </summary>
    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task CreateBanActivateDeactivate_StatusTransitionsCorrect(int _)
    {
        var created = await Sut.CreateAsync(new CreateCustomerRequest { Name = "Клиент", Email = "client@example.com" });
        Assert.Equal(CustomerStatus.Active, created.Status);

        var banned = await Sut.BanAsync(created.Id);
        Assert.Equal(CustomerStatus.Banned, banned.Status);

        var activated = await Sut.ActivateAsync(created.Id);
        Assert.Equal(CustomerStatus.Active, activated.Status);

        var deactivated = await Sut.DeactivateAsync(created.Id);
        Assert.Equal(CustomerStatus.Inactive, deactivated.Status);

        var fetched = await Sut.GetByIdAsync(created.Id);
        Assert.Equal(CustomerStatus.Inactive, fetched.Status);

        // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
        for (var i = 0; i < 3; i++)
        {
            var extra = await Sut.CreateAsync(new CreateCustomerRequest { Name = $"Доп {i}", Email = $"pad{i}@example.com" });
            await Sut.BanAsync(extra.Id);
            await Sut.GetByIdAsync(extra.Id);
        }
        await Sut.GetAllAsync();
    }
}
