namespace FastIntegrationTests.Tests.IntegreSQL.Customers;

/// <summary>
/// Тесты сервисного уровня: Create (ошибки), Update, Delete, статусные переходы для CustomerService.
/// Каждый тест получает клон шаблонной БД IntegreSQL (~5 мс).
/// </summary>
public class CustomerServiceUdTests : AppServiceTestBase
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
    public async Task CreateAsync_WhenDuplicateEmail_ThrowsDuplicateValueException(int _)
    {
        await Sut.CreateAsync(new CreateCustomerRequest { Name = "Иван", Email = "dup@example.com" });

        await Assert.ThrowsAsync<DuplicateValueException>(
            () => Sut.CreateAsync(new CreateCustomerRequest { Name = "Другой", Email = "dup@example.com" }));
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task UpdateAsync_UpdatesFields(int _)
    {
        var created = await Sut.CreateAsync(new CreateCustomerRequest { Name = "Старое", Email = "old@example.com" });

        var updated = await Sut.UpdateAsync(created.Id, new UpdateCustomerRequest { Name = "Новое", Email = "new@example.com", Phone = "+79999999999" });

        Assert.Equal("Новое", updated.Name);
        Assert.Equal("new@example.com", updated.Email);
        Assert.Equal("+79999999999", updated.Phone);

        var fetched = await Sut.GetByIdAsync(created.Id);
        Assert.Equal("Новое", fetched.Name);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task UpdateAsync_WhenNotFound_ThrowsNotFoundException(int _)
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => Sut.UpdateAsync(Guid.NewGuid(), new UpdateCustomerRequest { Name = "Любое", Email = "any@example.com" }));
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task DeleteAsync_RemovesCustomer(int _)
    {
        var created = await Sut.CreateAsync(new CreateCustomerRequest { Name = "Удаляемый", Email = "del@example.com" });

        await Sut.DeleteAsync(created.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => Sut.GetByIdAsync(created.Id));
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task BanAsync_BansCustomer(int _)
    {
        var created = await Sut.CreateAsync(new CreateCustomerRequest { Name = "Нарушитель", Email = "ban@example.com" });

        var banned = await Sut.BanAsync(created.Id);

        Assert.Equal(CustomerStatus.Banned, banned.Status);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task BanAsync_WhenAlreadyBanned_ThrowsInvalidStatusTransitionException(int _)
    {
        var created = await Sut.CreateAsync(new CreateCustomerRequest { Name = "Уже забанен", Email = "banned@example.com" });
        await Sut.BanAsync(created.Id);

        await Assert.ThrowsAsync<InvalidStatusTransitionException>(() => Sut.BanAsync(created.Id));
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task ActivateAsync_ActivatesCustomer(int _)
    {
        var created = await Sut.CreateAsync(new CreateCustomerRequest { Name = "Неактивный", Email = "inactive@example.com" });
        await Sut.DeactivateAsync(created.Id);

        var activated = await Sut.ActivateAsync(created.Id);

        Assert.Equal(CustomerStatus.Active, activated.Status);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task DeactivateAsync_DeactivatesCustomer(int _)
    {
        var created = await Sut.CreateAsync(new CreateCustomerRequest { Name = "Активный", Email = "active@example.com" });

        var deactivated = await Sut.DeactivateAsync(created.Id);

        Assert.Equal(CustomerStatus.Inactive, deactivated.Status);
    }
}
