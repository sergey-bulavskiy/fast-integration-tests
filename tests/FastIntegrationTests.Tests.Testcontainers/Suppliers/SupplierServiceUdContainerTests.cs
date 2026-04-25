namespace FastIntegrationTests.Tests.Testcontainers.Suppliers;

/// <summary>
/// Тесты сервисного уровня: Create (ошибки), Update, Delete, Activate, Deactivate для SupplierService.
/// Каждый тест создаёт изолированную БД в реальном контейнере PostgreSQL.
/// </summary>
public class SupplierServiceUdContainerTests : IAsyncLifetime, IClassFixture<ContainerFixture>
{
    private readonly ContainerFixture _fixture;
    private ShopDbContext _context = null!;
    private ISupplierService Sut = null!;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="SupplierServiceUdContainerTests"/>.
    /// </summary>
    /// <param name="fixture">Запущенный контейнер с СУБД.</param>
    public SupplierServiceUdContainerTests(ContainerFixture fixture) => _fixture = fixture;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        _context = await new TestDbFactory(_fixture).CreateAsync();
        Sut = new SupplierService(new SupplierRepository(_context));
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task CreateAsync_WhenDuplicateEmail_ThrowsDuplicateValueException(int _)
    {
        await Sut.CreateAsync(new CreateSupplierRequest { Name = "ООО Альфа", ContactEmail = "dup@vendor.com", Country = "Россия" });

        await Assert.ThrowsAsync<DuplicateValueException>(
            () => Sut.CreateAsync(new CreateSupplierRequest { Name = "Другой", ContactEmail = "dup@vendor.com", Country = "Беларусь" }));
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task UpdateAsync_UpdatesFields(int _)
    {
        var created = await Sut.CreateAsync(new CreateSupplierRequest { Name = "Старое", ContactEmail = "old@vendor.com", Country = "Россия" });

        var updated = await Sut.UpdateAsync(created.Id, new UpdateSupplierRequest { Name = "Новое", ContactEmail = "new@vendor.com", Country = "Казахстан" });

        Assert.Equal("Новое", updated.Name);
        Assert.Equal("new@vendor.com", updated.ContactEmail);
        Assert.Equal("Казахстан", updated.Country);

        var fetched = await Sut.GetByIdAsync(created.Id);
        Assert.Equal("Новое", fetched.Name);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task UpdateAsync_WhenNotFound_ThrowsNotFoundException(int _)
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => Sut.UpdateAsync(Guid.NewGuid(), new UpdateSupplierRequest { Name = "Любое", ContactEmail = "any@vendor.com", Country = "Россия" }));
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task DeleteAsync_RemovesSupplier(int _)
    {
        var created = await Sut.CreateAsync(new CreateSupplierRequest { Name = "Удаляемый", ContactEmail = "del@vendor.com", Country = "Россия" });

        await Sut.DeleteAsync(created.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => Sut.GetByIdAsync(created.Id));
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task ActivateAsync_ActivatesSupplier(int _)
    {
        var created = await Sut.CreateAsync(new CreateSupplierRequest { Name = "Неактивный", ContactEmail = "inactive@vendor.com", Country = "Россия" });
        await Sut.DeactivateAsync(created.Id);

        var activated = await Sut.ActivateAsync(created.Id);

        Assert.True(activated.IsActive);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task DeactivateAsync_DeactivatesSupplier(int _)
    {
        var created = await Sut.CreateAsync(new CreateSupplierRequest { Name = "Активный", ContactEmail = "active@vendor.com", Country = "Россия" });

        var deactivated = await Sut.DeactivateAsync(created.Id);

        Assert.False(deactivated.IsActive);
    }

    /// <summary>
    /// Создаёт несколько поставщиков, проверяет GetAll и GetById каждого.
    /// </summary>
    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task CreateMultiple_GetAll_GetByIdEach_ReturnsConsistentData(int _)
    {
        var a = await Sut.CreateAsync(new CreateSupplierRequest { Name = "ООО Альфа", ContactEmail = "alpha@example.com", Country = "Россия" });
        var b = await Sut.CreateAsync(new CreateSupplierRequest { Name = "ИП Бета", ContactEmail = "beta@example.com", Country = "Беларусь" });
        var c = await Sut.CreateAsync(new CreateSupplierRequest { Name = "ЗАО Гамма", ContactEmail = "gamma@example.com", Country = "Казахстан" });

        var all = await Sut.GetAllAsync();
        Assert.Equal(3, all.Count);
        Assert.Equal("ООО Альфа", (await Sut.GetByIdAsync(a.Id)).Name);
        Assert.Equal("ИП Бета", (await Sut.GetByIdAsync(b.Id)).Name);
        Assert.Equal("ЗАО Гамма", (await Sut.GetByIdAsync(c.Id)).Name);

        // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
        for (var i = 0; i < 4; i++)
        {
            var extra = await Sut.CreateAsync(new CreateSupplierRequest { Name = $"Доп {i}", ContactEmail = $"extra{i}@example.com", Country = "РФ" });
            await Sut.GetByIdAsync(extra.Id);
        }
        await Sut.GetAllAsync();
    }

    /// <summary>
    /// Создаёт поставщика, обновляет поля, деактивирует, активирует — проверяет все переходы.
    /// </summary>
    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task CreateUpdateDeactivateActivate_AllPersist(int _)
    {
        var created = await Sut.CreateAsync(new CreateSupplierRequest { Name = "ООО Старт", ContactEmail = "start@example.com", Country = "Россия" });
        Assert.True(created.IsActive);

        var updated = await Sut.UpdateAsync(created.Id, new UpdateSupplierRequest { Name = "ООО Финиш", ContactEmail = "start@example.com", Country = "Беларусь" });
        Assert.Equal("ООО Финиш", updated.Name);
        Assert.Equal("Беларусь", updated.Country);

        var deactivated = await Sut.DeactivateAsync(created.Id);
        Assert.False(deactivated.IsActive);

        var activated = await Sut.ActivateAsync(created.Id);
        Assert.True(activated.IsActive);

        var fetched = await Sut.GetByIdAsync(created.Id);
        Assert.Equal("ООО Финиш", fetched.Name);
        Assert.True(fetched.IsActive);

        // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
        for (var i = 0; i < 3; i++)
        {
            var extra = await Sut.CreateAsync(new CreateSupplierRequest { Name = $"Доп {i}", ContactEmail = $"pad{i}@example.com", Country = "РФ" });
            await Sut.DeactivateAsync(extra.Id);
            await Sut.GetByIdAsync(extra.Id);
        }
        await Sut.GetAllAsync();
    }
}
