namespace FastIntegrationTests.Tests.Respawn.Reviews;

/// <summary>
/// Тесты HTTP-уровня: Create, Delete, Approve, Reject для ReviewsController.
/// Каждый тест сбрасывает данные через Respawn (~1 мс), схема и TestServer сохраняются.
/// </summary>
public class ReviewsApiUdRespawnTests : RespawnApiTestBase
{
    /// <summary>Создаёт новый экземпляр <see cref="ReviewsApiUdRespawnTests"/>.</summary>
    /// <param name="fixture">Фикстура с контейнером и Respawner.</param>
    public ReviewsApiUdRespawnTests(RespawnApiFixture fixture) : base(fixture) { }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task Create_ValidRequest_Returns201WithLocationHeader(int _)
    {
        var request = new CreateReviewRequest { Title = "Отлично", Body = "Всё понравилось", Rating = 5 };

        var response = await Client.PostAsJsonAsync("/api/reviews", request);
        var item = await response.Content.ReadFromJsonAsync<ReviewDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.NotEqual(Guid.Empty, item!.Id);
        Assert.Equal("Отлично", item.Title);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task Create_WhenInvalidRating_Returns422(int _)
    {
        var response = await Client.PostAsJsonAsync("/api/reviews",
            new CreateReviewRequest { Title = "Плохо", Body = "Текст", Rating = 6 });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task Delete_WhenExists_Returns204(int _)
    {
        var created = await CreateReviewAsync("Удаляемый", 3);

        var response = await Client.DeleteAsync($"/api/reviews/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task Approve_WhenPending_Returns204(int _)
    {
        var created = await CreateReviewAsync("На модерации", 4);

        var response = await Client.PostAsync($"/api/reviews/{created.Id}/approve", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task Approve_WhenNotPending_Returns422(int _)
    {
        var created = await CreateReviewAsync("Одобренный", 4);
        await Client.PostAsync($"/api/reviews/{created.Id}/approve", null);

        var response = await Client.PostAsync($"/api/reviews/{created.Id}/approve", null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task Reject_WhenPending_Returns204(int _)
    {
        var created = await CreateReviewAsync("Спам", 1);

        var response = await Client.PostAsync($"/api/reviews/{created.Id}/reject", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task Reject_WhenNotPending_Returns422(int _)
    {
        var created = await CreateReviewAsync("Отклонённый", 1);
        await Client.PostAsync($"/api/reviews/{created.Id}/reject", null);

        var response = await Client.PostAsync($"/api/reviews/{created.Id}/reject", null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
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
