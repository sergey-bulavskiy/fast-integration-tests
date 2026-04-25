namespace FastIntegrationTests.Tests.Respawn.Reviews;

/// <summary>
/// Тесты HTTP-уровня: GetAll, GetById для ReviewsController.
/// Каждый тест сбрасывает данные через Respawn (~1 мс), схема и TestServer сохраняются.
/// </summary>
public class ReviewsApiCrRespawnTests : RespawnApiTestBase
{
    /// <summary>Создаёт новый экземпляр <see cref="ReviewsApiCrRespawnTests"/>.</summary>
    /// <param name="fixture">Фикстура с контейнером и Respawner.</param>
    public ReviewsApiCrRespawnTests(RespawnApiFixture fixture) : base(fixture) { }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetAll_WhenEmpty_Returns200WithEmptyArray(int _)
    {
        var response = await Client.GetAsync("/api/reviews");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<List<ReviewDto>>();
        Assert.Empty(items!);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetAll_WhenExist_Returns200WithReviews(int _)
    {
        await CreateReviewAsync("Отлично", 5);
        await CreateReviewAsync("Хорошо", 4);

        var response = await Client.GetAsync("/api/reviews");
        var items = await response.Content.ReadFromJsonAsync<List<ReviewDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, items!.Count);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetById_WhenExists_Returns200WithReview(int _)
    {
        var created = await CreateReviewAsync("Хороший товар", 4);

        var response = await Client.GetAsync($"/api/reviews/{created.Id}");
        var item = await response.Content.ReadFromJsonAsync<ReviewDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(created.Id, item!.Id);
        Assert.Equal("Хороший товар", item.Title);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetById_WhenNotFound_Returns404(int _)
    {
        var response = await Client.GetAsync($"/api/reviews/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Создаёт несколько отзывов через API, проверяет GetAll и GetById каждого.
    /// </summary>
    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task CreateMultiple_GetAll_GetByIdEach_ReturnsConsistentData(int _)
    {
        var a = await CreateReviewAsync("Отлично", 5);
        var b = await CreateReviewAsync("Хорошо", 4);
        var c = await CreateReviewAsync("Средне", 3);

        var all = await Client.GetAsync("/api/reviews");
        var list = await all.Content.ReadFromJsonAsync<List<ReviewDto>>();
        Assert.Equal(3, list!.Count);

        var fa = await (await Client.GetAsync($"/api/reviews/{a.Id}")).Content.ReadFromJsonAsync<ReviewDto>();
        Assert.Equal("Отлично", fa!.Title);
        Assert.Equal(ReviewStatus.Pending, fa.Status);

        // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
        for (var i = 0; i < 4; i++)
        {
            var extra = await CreateReviewAsync($"Отзыв {i}", 3 + i % 3);
            await Client.GetAsync($"/api/reviews/{extra.Id}");
        }
        await Client.GetAsync("/api/reviews");
    }

    /// <summary>
    /// Создаёт два отзыва, один одобряет, второй отклоняет через API, первый удаляет.
    /// </summary>
    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task CreateApproveReject_ThenDelete_LifecycleCorrect(int _)
    {
        var toApprove = await CreateReviewAsync("Одобрить", 5);
        var toReject = await CreateReviewAsync("Отклонить", 1);

        Assert.Equal(HttpStatusCode.NoContent, (await Client.PostAsync($"/api/reviews/{toApprove.Id}/approve", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Client.PostAsync($"/api/reviews/{toReject.Id}/reject", null)).StatusCode);

        var approved = await (await Client.GetAsync($"/api/reviews/{toApprove.Id}")).Content.ReadFromJsonAsync<ReviewDto>();
        Assert.Equal(ReviewStatus.Approved, approved!.Status);

        Assert.Equal(HttpStatusCode.NoContent, (await Client.DeleteAsync($"/api/reviews/{toApprove.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync($"/api/reviews/{toApprove.Id}")).StatusCode);

        // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
        for (var i = 0; i < 4; i++)
        {
            var extra = await CreateReviewAsync($"Доп {i}", 4);
            await Client.PostAsync($"/api/reviews/{extra.Id}/approve", null);
            await Client.GetAsync($"/api/reviews/{extra.Id}");
        }
        await Client.GetAsync("/api/reviews");
    }

    // --- helpers ---

    /// <summary>
    /// Создаёт отзыв через API и возвращает его DTO.
    /// </summary>
    /// <param name="title">Заголовок отзыва.</param>
    /// <param name="rating">Рейтинг (1–5).</param>
    /// <param name="ct">Токен отмены операции.</param>
    private async Task<ReviewDto> CreateReviewAsync(string title, int rating, CancellationToken ct = default)
    {
        var response = await Client.PostAsJsonAsync("/api/reviews",
            new CreateReviewRequest { Title = title, Body = "Текст отзыва", Rating = rating }, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ReviewDto>(ct))!;
    }
}
