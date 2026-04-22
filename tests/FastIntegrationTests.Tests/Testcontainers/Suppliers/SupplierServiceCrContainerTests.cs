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

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetAllAsync_WhenNoSuppliers_ReturnsEmptyList(int _)
    {
        var result = await Sut.GetAllAsync();

        Assert.Empty(result);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetAllAsync_WhenSuppliersExist_ReturnsAllSuppliers(int _)
    {
        await Sut.CreateAsync(new CreateSupplierRequest { Name = "ООО Альфа", ContactEmail = "alpha@vendor.com", Country = "Россия" });
        await Sut.CreateAsync(new CreateSupplierRequest { Name = "ООО Бета", ContactEmail = "beta@vendor.com", Country = "Беларусь" });

        var result = await Sut.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetByIdAsync_WhenExists_ReturnsSupplier(int _)
    {
        var created = await Sut.CreateAsync(new CreateSupplierRequest { Name = "ООО Гамма", ContactEmail = "gamma@vendor.com", Country = "Казахстан" });

        var result = await Sut.GetByIdAsync(created.Id);

        Assert.Equal(created.Id, result.Id);
        Assert.Equal("ООО Гамма", result.Name);
        Assert.Equal("gamma@vendor.com", result.ContactEmail);
        Assert.Equal("Казахстан", result.Country);
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
        var result = await Sut.CreateAsync(new CreateSupplierRequest { Name = "ООО Дельта", ContactEmail = "delta@vendor.com", Country = "Россия" });

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("ООО Дельта", result.Name);
        Assert.Equal("delta@vendor.com", result.ContactEmail);
        Assert.True(result.IsActive);
        Assert.True(result.CreatedAt > DateTime.UtcNow.AddSeconds(-5));
    }
}
