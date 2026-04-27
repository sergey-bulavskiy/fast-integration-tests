namespace FastIntegrationTests.Tests.Respawn.Discounts;

/// <summary>
/// Тесты сервисного уровня: GetAll, GetById, Create, Update, Delete, Activate, Deactivate для DiscountService.
/// Каждый тест сбрасывает данные через Respawn (~1 мс), схема сохраняется.
/// </summary>
public class DiscountServiceRespawnTests : RespawnServiceTestBase
{
    private IDiscountService Sut = null!;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="DiscountServiceRespawnTests"/>.
    /// </summary>
    /// <param name="fixture">Фикстура с контейнером и Respawner.</param>
    public DiscountServiceRespawnTests(RespawnFixture fixture) : base(fixture) { }

    /// <inheritdoc/>
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        Sut = new DiscountService(new DiscountRepository(Context));
    }

    [Fact]
    public async Task GetAllAsync_WhenNoDiscounts_ReturnsEmptyList()
    {
        var result = await Sut.GetAllAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_WhenDiscountsExist_ReturnsAllDiscounts()
    {
        await Sut.CreateAsync(new CreateDiscountRequest { Code = "SALE10", DiscountPercent = 10 });
        await Sut.CreateAsync(new CreateDiscountRequest { Code = "SALE20", DiscountPercent = 20 });

        var result = await Sut.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsDiscount()
    {
        var created = await Sut.CreateAsync(new CreateDiscountRequest { Code = "PROMO15", DiscountPercent = 15 });

        var result = await Sut.GetByIdAsync(created.Id);

        Assert.Equal(created.Id, result.Id);
        Assert.Equal("PROMO15", result.Code);
        Assert.Equal(15, result.DiscountPercent);
        Assert.False(result.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Sut.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateAsync_PersistsAndReturns()
    {
        var result = await Sut.CreateAsync(new CreateDiscountRequest { Code = "WELCOME5", DiscountPercent = 5 });

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("WELCOME5", result.Code);
        Assert.Equal(5, result.DiscountPercent);
        Assert.False(result.IsActive);
        Assert.True(result.CreatedAt > DateTime.UtcNow.AddSeconds(-5));
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
}
