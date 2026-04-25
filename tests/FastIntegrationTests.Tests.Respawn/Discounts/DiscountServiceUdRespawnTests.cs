namespace FastIntegrationTests.Tests.Respawn.Discounts;

/// <summary>
/// Тесты сервисного уровня: Create (ошибки), Update, Delete, Activate, Deactivate для DiscountService.
/// Каждый тест сбрасывает данные через Respawn (~1 мс), схема сохраняется.
/// </summary>
public class DiscountServiceUdRespawnTests : RespawnServiceTestBase
{
    private IDiscountService Sut = null!;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="DiscountServiceUdRespawnTests"/>.
    /// </summary>
    /// <param name="fixture">Фикстура с контейнером и Respawner.</param>
    public DiscountServiceUdRespawnTests(RespawnFixture fixture) : base(fixture) { }

    /// <inheritdoc/>
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        Sut = new DiscountService(new DiscountRepository(Context));
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
