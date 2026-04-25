namespace FastIntegrationTests.Tests.Respawn.Discounts;

/// <summary>
/// Тесты HTTP-уровня: Create, Update, Delete, Activate, Deactivate для DiscountsController.
/// Каждый тест сбрасывает данные через Respawn (~1 мс), схема и TestServer сохраняются.
/// </summary>
public class DiscountsApiUdRespawnTests : RespawnApiTestBase
{
    /// <summary>Создаёт новый экземпляр <see cref="DiscountsApiUdRespawnTests"/>.</summary>
    /// <param name="fixture">Фикстура с контейнером и Respawner.</param>
    public DiscountsApiUdRespawnTests(RespawnApiFixture fixture) : base(fixture) { }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task Create_ValidRequest_Returns201WithLocationHeader(int _)
    {
        var request = new CreateDiscountRequest { Code = "SALE10", DiscountPercent = 10 };

        var response = await Client.PostAsJsonAsync("/api/discounts", request);
        var item = await response.Content.ReadFromJsonAsync<DiscountDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.NotEqual(Guid.Empty, item!.Id);
        Assert.Equal("SALE10", item.Code);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task Create_WhenInvalidPercent_Returns422(int _)
    {
        var response = await Client.PostAsJsonAsync("/api/discounts",
            new CreateDiscountRequest { Code = "INVALID", DiscountPercent = 0 });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task Create_WhenDuplicateCode_Returns409(int _)
    {
        await Client.PostAsJsonAsync("/api/discounts", new CreateDiscountRequest { Code = "DUP", DiscountPercent = 10 });

        var response = await Client.PostAsJsonAsync("/api/discounts", new CreateDiscountRequest { Code = "DUP", DiscountPercent = 20 });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task Update_WhenExists_Returns200WithUpdatedFields(int _)
    {
        var created = await CreateDiscountAsync("OLD10", 10);

        var response = await Client.PutAsJsonAsync($"/api/discounts/{created.Id}",
            new UpdateDiscountRequest { Code = "NEW25", DiscountPercent = 25 });
        var updated = await response.Content.ReadFromJsonAsync<DiscountDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("NEW25", updated!.Code);
        Assert.Equal(25, updated.DiscountPercent);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task Update_WhenNotFound_Returns404(int _)
    {
        var response = await Client.PutAsJsonAsync($"/api/discounts/{Guid.NewGuid()}",
            new UpdateDiscountRequest { Code = "ANY", DiscountPercent = 10 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task Delete_WhenExists_Returns204(int _)
    {
        var created = await CreateDiscountAsync("DEL10", 10);

        var response = await Client.DeleteAsync($"/api/discounts/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task Activate_WhenExists_Returns204(int _)
    {
        var created = await CreateDiscountAsync("ACT10", 10);

        var response = await Client.PostAsync($"/api/discounts/{created.Id}/activate", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task Deactivate_WhenExists_Returns204(int _)
    {
        var created = await CreateDiscountAsync("DEACT10", 10);
        await Client.PostAsync($"/api/discounts/{created.Id}/activate", null);

        var response = await Client.PostAsync($"/api/discounts/{created.Id}/deactivate", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
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
