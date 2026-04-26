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

    [Fact]
    public async Task CreateAsync_WhenInvalidPercent_ThrowsInvalidDiscountPercentException()
    {
        await Assert.ThrowsAsync<InvalidDiscountPercentException>(
            () => Sut.CreateAsync(new CreateDiscountRequest { Code = "INVALID", DiscountPercent = 0 }));
    }

    [Fact]
    public async Task CreateAsync_WhenDuplicateCode_ThrowsDuplicateValueException()
    {
        await Sut.CreateAsync(new CreateDiscountRequest { Code = "DUP", DiscountPercent = 10 });

        await Assert.ThrowsAsync<DuplicateValueException>(
            () => Sut.CreateAsync(new CreateDiscountRequest { Code = "DUP", DiscountPercent = 20 }));
    }

    [Fact]
    public async Task UpdateAsync_UpdatesFields()
    {
        var created = await Sut.CreateAsync(new CreateDiscountRequest { Code = "OLD10", DiscountPercent = 10 });

        var updated = await Sut.UpdateAsync(created.Id, new UpdateDiscountRequest { Code = "NEW25", DiscountPercent = 25 });

        Assert.Equal("NEW25", updated.Code);
        Assert.Equal(25, updated.DiscountPercent);

        var fetched = await Sut.GetByIdAsync(created.Id);
        Assert.Equal("NEW25", fetched.Code);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => Sut.UpdateAsync(Guid.NewGuid(), new UpdateDiscountRequest { Code = "ANY", DiscountPercent = 10 }));
    }

    [Fact]
    public async Task DeleteAsync_RemovesDiscount()
    {
        var created = await Sut.CreateAsync(new CreateDiscountRequest { Code = "DEL10", DiscountPercent = 10 });

        await Sut.DeleteAsync(created.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => Sut.GetByIdAsync(created.Id));
    }

    [Fact]
    public async Task ActivateAsync_ActivatesDiscount()
    {
        var created = await Sut.CreateAsync(new CreateDiscountRequest { Code = "ACT10", DiscountPercent = 10 });

        var activated = await Sut.ActivateAsync(created.Id);

        Assert.True(activated.IsActive);
    }

    [Fact]
    public async Task DeactivateAsync_DeactivatesDiscount()
    {
        var created = await Sut.CreateAsync(new CreateDiscountRequest { Code = "DEACT10", DiscountPercent = 10 });
        await Sut.ActivateAsync(created.Id);

        var deactivated = await Sut.DeactivateAsync(created.Id);

        Assert.False(deactivated.IsActive);
    }

    /// <summary>
    /// Создаёт несколько скидок, проверяет GetAll и GetById каждой.
    /// </summary>
    [Fact]
    public async Task CreateMultiple_GetAll_GetByIdEach_ReturnsConsistentData()
    {
        var a = await Sut.CreateAsync(new CreateDiscountRequest { Code = "SALE10", DiscountPercent = 10 });
        var b = await Sut.CreateAsync(new CreateDiscountRequest { Code = "SALE20", DiscountPercent = 20 });
        var c = await Sut.CreateAsync(new CreateDiscountRequest { Code = "SALE30", DiscountPercent = 30 });

        var all = await Sut.GetAllAsync();
        Assert.Equal(3, all.Count);
        Assert.Equal("SALE10", (await Sut.GetByIdAsync(a.Id)).Code);
        Assert.Equal("SALE20", (await Sut.GetByIdAsync(b.Id)).Code);
        Assert.Equal("SALE30", (await Sut.GetByIdAsync(c.Id)).Code);

        // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
        for (var i = 0; i < 4; i++)
        {
            var extra = await Sut.CreateAsync(new CreateDiscountRequest { Code = $"EXTRA{i:00}", DiscountPercent = 5 + i });
            await Sut.GetByIdAsync(extra.Id);
        }
        await Sut.GetAllAsync();
    }

    /// <summary>
    /// Создаёт скидку, активирует, деактивирует, обновляет — проверяет каждый шаг.
    /// </summary>
    [Fact]
    public async Task CreateActivateDeactivateUpdate_AllPersist()
    {
        var created = await Sut.CreateAsync(new CreateDiscountRequest { Code = "START10", DiscountPercent = 10 });
        Assert.False(created.IsActive);

        var activated = await Sut.ActivateAsync(created.Id);
        Assert.True(activated.IsActive);

        var deactivated = await Sut.DeactivateAsync(created.Id);
        Assert.False(deactivated.IsActive);

        var updated = await Sut.UpdateAsync(created.Id, new UpdateDiscountRequest { Code = "FINISH25", DiscountPercent = 25 });
        Assert.Equal("FINISH25", updated.Code);
        Assert.Equal(25, updated.DiscountPercent);

        var fetched = await Sut.GetByIdAsync(created.Id);
        Assert.Equal("FINISH25", fetched.Code);
        Assert.False(fetched.IsActive);

        // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
        for (var i = 0; i < 3; i++)
        {
            var extra = await Sut.CreateAsync(new CreateDiscountRequest { Code = $"PAD{i:00}", DiscountPercent = 5 + i });
            await Sut.ActivateAsync(extra.Id);
            await Sut.GetByIdAsync(extra.Id);
        }
        await Sut.GetAllAsync();
    }
}
