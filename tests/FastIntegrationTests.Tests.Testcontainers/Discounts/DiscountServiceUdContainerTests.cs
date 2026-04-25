namespace FastIntegrationTests.Tests.Testcontainers.Discounts;

/// <summary>
/// Тесты сервисного уровня: Create (ошибки), Update, Delete, Activate, Deactivate для DiscountService.
/// Каждый тест создаёт изолированную БД в реальном контейнере PostgreSQL.
/// </summary>
public class DiscountServiceUdContainerTests : IAsyncLifetime, IClassFixture<ContainerFixture>
{
    private readonly ContainerFixture _fixture;
    private ShopDbContext _context = null!;
    private IDiscountService Sut = null!;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="DiscountServiceUdContainerTests"/>.
    /// </summary>
    /// <param name="fixture">Запущенный контейнер с СУБД.</param>
    public DiscountServiceUdContainerTests(ContainerFixture fixture) => _fixture = fixture;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        _context = await new TestDbFactory(_fixture).CreateAsync();
        Sut = new DiscountService(new DiscountRepository(_context));
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task CreateAsync_WhenInvalidPercent_ThrowsInvalidDiscountPercentException(int _)
    {
        await Assert.ThrowsAsync<InvalidDiscountPercentException>(
            () => Sut.CreateAsync(new CreateDiscountRequest { Code = "INVALID", DiscountPercent = 0 }));
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task CreateAsync_WhenDuplicateCode_ThrowsDuplicateValueException(int _)
    {
        await Sut.CreateAsync(new CreateDiscountRequest { Code = "DUP", DiscountPercent = 10 });

        await Assert.ThrowsAsync<DuplicateValueException>(
            () => Sut.CreateAsync(new CreateDiscountRequest { Code = "DUP", DiscountPercent = 20 }));
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task UpdateAsync_UpdatesFields(int _)
    {
        var created = await Sut.CreateAsync(new CreateDiscountRequest { Code = "OLD10", DiscountPercent = 10 });

        var updated = await Sut.UpdateAsync(created.Id, new UpdateDiscountRequest { Code = "NEW25", DiscountPercent = 25 });

        Assert.Equal("NEW25", updated.Code);
        Assert.Equal(25, updated.DiscountPercent);

        var fetched = await Sut.GetByIdAsync(created.Id);
        Assert.Equal("NEW25", fetched.Code);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task UpdateAsync_WhenNotFound_ThrowsNotFoundException(int _)
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => Sut.UpdateAsync(Guid.NewGuid(), new UpdateDiscountRequest { Code = "ANY", DiscountPercent = 10 }));
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task DeleteAsync_RemovesDiscount(int _)
    {
        var created = await Sut.CreateAsync(new CreateDiscountRequest { Code = "DEL10", DiscountPercent = 10 });

        await Sut.DeleteAsync(created.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => Sut.GetByIdAsync(created.Id));
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task ActivateAsync_ActivatesDiscount(int _)
    {
        var created = await Sut.CreateAsync(new CreateDiscountRequest { Code = "ACT10", DiscountPercent = 10 });

        var activated = await Sut.ActivateAsync(created.Id);

        Assert.True(activated.IsActive);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task DeactivateAsync_DeactivatesDiscount(int _)
    {
        var created = await Sut.CreateAsync(new CreateDiscountRequest { Code = "DEACT10", DiscountPercent = 10 });
        await Sut.ActivateAsync(created.Id);

        var deactivated = await Sut.DeactivateAsync(created.Id);

        Assert.False(deactivated.IsActive);
    }
}
