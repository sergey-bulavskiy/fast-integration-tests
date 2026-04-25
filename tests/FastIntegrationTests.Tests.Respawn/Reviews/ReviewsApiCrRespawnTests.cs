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
