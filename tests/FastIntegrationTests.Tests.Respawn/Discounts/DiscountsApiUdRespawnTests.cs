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
