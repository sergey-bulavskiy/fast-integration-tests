namespace FastIntegrationTests.Tests.Testcontainers.Suppliers;

/// <summary>
/// Тесты сервисного уровня: GetAll, GetById, Create для SupplierService.
/// Каждый тест создаёт изолированную БД в реальном контейнере PostgreSQL.
/// </summary>
public class SupplierServiceCrContainerTests : IAsyncLifetime, IClassFixture<ContainerFixture>
{
    private readonly ContainerFixture _fixture;
    private ShopDbContext _context = null!;
    private ISupplierService Sut = null!;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="SupplierServiceCrContainerTests"/>.
    /// </summary>
    /// <param name="fixture">Запущенный контейнер с СУБД.</param>
    public SupplierServiceCrContainerTests(ContainerFixture fixture) => _fixture = fixture;

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

    [Fact]
    public async Task GetAllAsync_WhenNoSuppliers_ReturnsEmptyList()
    {
        var result = await Sut.GetAllAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_WhenSuppliersExist_ReturnsAllSuppliers()
    {
        await Sut.CreateAsync(new CreateSupplierRequest { Name = "ООО Альфа", ContactEmail = "alpha@vendor.com", Country = "Россия" });
        await Sut.CreateAsync(new CreateSupplierRequest { Name = "ООО Бета", ContactEmail = "beta@vendor.com", Country = "Беларусь" });

        var result = await Sut.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsSupplier()
    {
        var created = await Sut.CreateAsync(new CreateSupplierRequest { Name = "ООО Гамма", ContactEmail = "gamma@vendor.com", Country = "Казахстан" });

        var result = await Sut.GetByIdAsync(created.Id);

        Assert.Equal(created.Id, result.Id);
        Assert.Equal("ООО Гамма", result.Name);
        Assert.Equal("gamma@vendor.com", result.ContactEmail);
        Assert.Equal("Казахстан", result.Country);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Sut.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateAsync_PersistsAndReturns()
    {
        var result = await Sut.CreateAsync(new CreateSupplierRequest { Name = "ООО Дельта", ContactEmail = "delta@vendor.com", Country = "Россия" });

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("ООО Дельта", result.Name);
        Assert.Equal("delta@vendor.com", result.ContactEmail);
        Assert.True(result.IsActive);
        Assert.True(result.CreatedAt > DateTime.UtcNow.AddSeconds(-5));
    }

    /// <summary>
    /// Создаёт несколько поставщиков, проверяет GetAll и GetById каждого.
    /// </summary>
    [Fact]
    public async Task CreateMultiple_GetAll_GetByIdEach_ReturnsConsistentData()
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
    [Fact]
    public async Task CreateUpdateDeactivateActivate_AllPersist()
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
