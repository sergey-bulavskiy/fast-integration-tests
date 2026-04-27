namespace FastIntegrationTests.Tests.IntegreSQL.Customers;

/// <summary>
/// Тесты сервисного уровня: GetAll, GetById, Create, Update, Delete для CustomerService.
/// Каждый тест получает клон шаблонной БД IntegreSQL (~5 мс).
/// </summary>
public class CustomerServiceTests : AppServiceTestBase
{
    private ICustomerService Sut = null!;

    /// <inheritdoc/>
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        Sut = new CustomerService(new CustomerRepository(Context));
    }

    [Fact]
    public async Task GetAllAsync_WhenNoCustomers_ReturnsEmptyList()
    {
        var result = await Sut.GetAllAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_WhenCustomersExist_ReturnsAllCustomers()
    {
        await Sut.CreateAsync(new CreateCustomerRequest { Name = "Иван", Email = "ivan@example.com" });
        await Sut.CreateAsync(new CreateCustomerRequest { Name = "Мария", Email = "maria@example.com" });

        var result = await Sut.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsCustomer()
    {
        var created = await Sut.CreateAsync(new CreateCustomerRequest { Name = "Пётр", Email = "petr@example.com", Phone = "+79001234567" });

        var result = await Sut.GetByIdAsync(created.Id);

        Assert.Equal(created.Id, result.Id);
        Assert.Equal("Пётр", result.Name);
        Assert.Equal("petr@example.com", result.Email);
        Assert.Equal("+79001234567", result.Phone);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Sut.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateAsync_PersistsAndReturns()
    {
        var result = await Sut.CreateAsync(new CreateCustomerRequest { Name = "Анна", Email = "anna@example.com" });

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Анна", result.Name);
        Assert.Equal("anna@example.com", result.Email);
        Assert.Equal(CustomerStatus.Active, result.Status);
        Assert.True(result.CreatedAt > DateTime.UtcNow.AddSeconds(-5));
    }

    [Fact]
    public async Task CreateAsync_WhenDuplicateEmail_ThrowsDuplicateValueException()
    {
        await Sut.CreateAsync(new CreateCustomerRequest { Name = "Иван", Email = "dup@example.com" });

        await Assert.ThrowsAsync<DuplicateValueException>(
            () => Sut.CreateAsync(new CreateCustomerRequest { Name = "Другой", Email = "dup@example.com" }));
    }

    [Fact]
    public async Task UpdateAsync_UpdatesFields()
    {
        var created = await Sut.CreateAsync(new CreateCustomerRequest { Name = "Старое", Email = "old@example.com" });

        var updated = await Sut.UpdateAsync(created.Id, new UpdateCustomerRequest { Name = "Новое", Email = "new@example.com", Phone = "+79999999999" });

        Assert.Equal("Новое", updated.Name);
        Assert.Equal("new@example.com", updated.Email);
        Assert.Equal("+79999999999", updated.Phone);

        var fetched = await Sut.GetByIdAsync(created.Id);
        Assert.Equal("Новое", fetched.Name);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => Sut.UpdateAsync(Guid.NewGuid(), new UpdateCustomerRequest { Name = "Любое", Email = "any@example.com" }));
    }

    [Fact]
    public async Task DeleteAsync_RemovesCustomer()
    {
        var created = await Sut.CreateAsync(new CreateCustomerRequest { Name = "Удаляемый", Email = "del@example.com" });

        await Sut.DeleteAsync(created.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => Sut.GetByIdAsync(created.Id));
    }

    [Fact]
    public async Task BanAsync_BansCustomer()
    {
        var created = await Sut.CreateAsync(new CreateCustomerRequest { Name = "Нарушитель", Email = "ban@example.com" });

        var banned = await Sut.BanAsync(created.Id);

        Assert.Equal(CustomerStatus.Banned, banned.Status);
    }

    [Fact]
    public async Task BanAsync_WhenAlreadyBanned_ThrowsInvalidStatusTransitionException()
    {
        var created = await Sut.CreateAsync(new CreateCustomerRequest { Name = "Уже забанен", Email = "banned@example.com" });
        await Sut.BanAsync(created.Id);

        await Assert.ThrowsAsync<InvalidStatusTransitionException>(() => Sut.BanAsync(created.Id));
    }

    [Fact]
    public async Task ActivateAsync_ActivatesCustomer()
    {
        var created = await Sut.CreateAsync(new CreateCustomerRequest { Name = "Неактивный", Email = "inactive@example.com" });
        await Sut.DeactivateAsync(created.Id);

        var activated = await Sut.ActivateAsync(created.Id);

        Assert.Equal(CustomerStatus.Active, activated.Status);
    }

    [Fact]
    public async Task DeactivateAsync_DeactivatesCustomer()
    {
        var created = await Sut.CreateAsync(new CreateCustomerRequest { Name = "Активный", Email = "active@example.com" });

        var deactivated = await Sut.DeactivateAsync(created.Id);

        Assert.Equal(CustomerStatus.Inactive, deactivated.Status);
    }

    /// <summary>
    /// Создаёт несколько покупателей, проверяет GetAll и GetById каждого.
    /// </summary>
    [Fact]
    public async Task CreateMultiple_GetAll_GetByIdEach_ReturnsConsistentData()
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
    [Fact]
    public async Task CreateBanActivateDeactivate_StatusTransitionsCorrect()
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
