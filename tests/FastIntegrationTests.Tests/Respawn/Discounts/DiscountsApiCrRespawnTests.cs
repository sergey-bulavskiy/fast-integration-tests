namespace FastIntegrationTests.Tests.Respawn.Discounts;

/// <summary>
/// Тесты HTTP-уровня: GetAll, GetById для DiscountsController.
/// Каждый тест сбрасывает данные через Respawn (~1 мс), схема и TestServer сохраняются.
/// </summary>
public class DiscountsApiCrRespawnTests : RespawnApiTestBase
{
    /// <summary>Создаёт новый экземпляр <see cref="DiscountsApiCrRespawnTests"/>.</summary>
    /// <param name="fixture">Фикстура с контейнером и Respawner.</param>
    public DiscountsApiCrRespawnTests(RespawnApiFixture fixture) : base(fixture) { }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetAll_WhenEmpty_Returns200WithEmptyArray(int _)
    {
        var response = await Client.GetAsync("/api/discounts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<List<DiscountDto>>();
        Assert.Empty(items!);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetAll_WhenExist_Returns200WithDiscounts(int _)
    {
        await CreateDiscountAsync("SALE10", 10);
        await CreateDiscountAsync("SALE20", 20);

        var response = await Client.GetAsync("/api/discounts");
        var items = await response.Content.ReadFromJsonAsync<List<DiscountDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, items!.Count);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetById_WhenExists_Returns200WithDiscount(int _)
    {
        var created = await CreateDiscountAsync("PROMO15", 15);

        var response = await Client.GetAsync($"/api/discounts/{created.Id}");
        var item = await response.Content.ReadFromJsonAsync<DiscountDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(created.Id, item!.Id);
        Assert.Equal("PROMO15", item.Code);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetById_WhenNotFound_Returns404(int _)
    {
        var response = await Client.GetAsync($"/api/discounts/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- helpers ---

    /// <summary>
    /// Создаёт скидку через API и возвращает её DTO.
    /// </summary>
    /// <param name="code">Код скидки.</param>
    /// <param name="percent">Процент скидки.</param>
    /// <param name="ct">Токен отмены операции.</param>
    private async Task<DiscountDto> CreateDiscountAsync(string code, int percent, CancellationToken ct = default)
    {
        var response = await Client.PostAsJsonAsync("/api/discounts",
            new CreateDiscountRequest { Code = code, DiscountPercent = percent }, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DiscountDto>(ct))!;
    }
}
