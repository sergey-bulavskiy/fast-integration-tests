namespace FastIntegrationTests.Tests.IntegreSQL.Discounts;

/// <summary>
/// Тесты сервисного уровня: GetAll, GetById, Create для DiscountService.
/// Каждый тест получает клон шаблонной БД IntegreSQL (~5 мс).
/// </summary>
public class DiscountServiceCrTests : AppServiceTestBase
{
    private IDiscountService Sut = null!;

    /// <inheritdoc/>
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        Sut = new DiscountService(new DiscountRepository(Context));
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetAllAsync_WhenNoDiscounts_ReturnsEmptyList(int _)
    {
        var result = await Sut.GetAllAsync();

        Assert.Empty(result);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetAllAsync_WhenDiscountsExist_ReturnsAllDiscounts(int _)
    {
        await Sut.CreateAsync(new CreateDiscountRequest { Code = "SALE10", DiscountPercent = 10 });
        await Sut.CreateAsync(new CreateDiscountRequest { Code = "SALE20", DiscountPercent = 20 });

        var result = await Sut.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetByIdAsync_WhenExists_ReturnsDiscount(int _)
    {
        var created = await Sut.CreateAsync(new CreateDiscountRequest { Code = "PROMO15", DiscountPercent = 15 });

        var result = await Sut.GetByIdAsync(created.Id);

        Assert.Equal(created.Id, result.Id);
        Assert.Equal("PROMO15", result.Code);
        Assert.Equal(15, result.DiscountPercent);
        Assert.False(result.IsActive);
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
        var result = await Sut.CreateAsync(new CreateDiscountRequest { Code = "WELCOME5", DiscountPercent = 5 });

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("WELCOME5", result.Code);
        Assert.Equal(5, result.DiscountPercent);
        Assert.False(result.IsActive);
        Assert.True(result.CreatedAt > DateTime.UtcNow.AddSeconds(-5));
    }
}
