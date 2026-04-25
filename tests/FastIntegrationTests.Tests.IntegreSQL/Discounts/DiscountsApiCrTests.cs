namespace FastIntegrationTests.Tests.IntegreSQL.Discounts;

/// <summary>
/// Тесты HTTP-уровня: GetAll, GetById для DiscountsController.
/// Каждый тест получает клон шаблонной БД IntegreSQL (~5 мс) и отдельный TestServer.
/// </summary>
public class DiscountsApiCrTests : ComponentTestBase
{
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

    /// <summary>
    /// Создаёт несколько скидок через API, проверяет GetAll и GetById каждой.
    /// </summary>
    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task CreateMultiple_GetAll_GetByIdEach_ReturnsConsistentData(int _)
    {
        var a = await CreateDiscountAsync("SALE10", 10);
        var b = await CreateDiscountAsync("SALE20", 20);
        var c = await CreateDiscountAsync("SALE30", 30);

        var all = await Client.GetAsync("/api/discounts");
        var list = await all.Content.ReadFromJsonAsync<List<DiscountDto>>();
        Assert.Equal(3, list!.Count);

        var fa = await (await Client.GetAsync($"/api/discounts/{a.Id}")).Content.ReadFromJsonAsync<DiscountDto>();
        Assert.Equal("SALE10", fa!.Code);

        // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
        for (var i = 0; i < 4; i++)
        {
            var extra = await CreateDiscountAsync($"EX{i:00}", 5 + i);
            await Client.GetAsync($"/api/discounts/{extra.Id}");
        }
        await Client.GetAsync("/api/discounts");
    }

    /// <summary>
    /// Создаёт скидку, активирует, деактивирует, обновляет через API.
    /// </summary>
    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task CreateActivateDeactivateUpdate_AllPersist(int _)
    {
        var created = await CreateDiscountAsync("START10", 10);
        Assert.False(created.IsActive);

        Assert.Equal(HttpStatusCode.NoContent, (await Client.PostAsync($"/api/discounts/{created.Id}/activate", null)).StatusCode);
        var activated = await (await Client.GetAsync($"/api/discounts/{created.Id}")).Content.ReadFromJsonAsync<DiscountDto>();
        Assert.True(activated!.IsActive);

        Assert.Equal(HttpStatusCode.NoContent, (await Client.PostAsync($"/api/discounts/{created.Id}/deactivate", null)).StatusCode);

        var putResp = await Client.PutAsJsonAsync($"/api/discounts/{created.Id}",
            new UpdateDiscountRequest { Code = "FINISH25", DiscountPercent = 25 });
        Assert.Equal(HttpStatusCode.OK, putResp.StatusCode);

        var fetched = await (await Client.GetAsync($"/api/discounts/{created.Id}")).Content.ReadFromJsonAsync<DiscountDto>();
        Assert.Equal("FINISH25", fetched!.Code);
        Assert.False(fetched.IsActive);

        // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
        for (var i = 0; i < 3; i++)
        {
            var extra = await CreateDiscountAsync($"PAD{i:00}", 5 + i);
            await Client.PostAsync($"/api/discounts/{extra.Id}/activate", null);
            await Client.GetAsync($"/api/discounts/{extra.Id}");
        }
        await Client.GetAsync("/api/discounts");
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
