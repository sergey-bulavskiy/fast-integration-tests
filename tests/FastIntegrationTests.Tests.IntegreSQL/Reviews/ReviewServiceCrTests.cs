namespace FastIntegrationTests.Tests.IntegreSQL.Reviews;

/// <summary>
/// Тесты сервисного уровня: GetAll, GetById, Create для ReviewService.
/// Каждый тест получает клон шаблонной БД IntegreSQL (~5 мс).
/// </summary>
public class ReviewServiceCrTests : AppServiceTestBase
{
    private IReviewService Sut = null!;

    /// <inheritdoc/>
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        Sut = new ReviewService(new ReviewRepository(Context));
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetAllAsync_WhenNoReviews_ReturnsEmptyList(int _)
    {
        var result = await Sut.GetAllAsync();

        Assert.Empty(result);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetAllAsync_WhenReviewsExist_ReturnsAllReviews(int _)
    {
        await Sut.CreateAsync(new CreateReviewRequest { Title = "Отлично", Body = "Всё понравилось", Rating = 5 });
        await Sut.CreateAsync(new CreateReviewRequest { Title = "Неплохо", Body = "В целом хорошо", Rating = 4 });

        var result = await Sut.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task GetByIdAsync_WhenExists_ReturnsReview(int _)
    {
        var created = await Sut.CreateAsync(new CreateReviewRequest { Title = "Хороший товар", Body = "Рекомендую", Rating = 4 });

        var result = await Sut.GetByIdAsync(created.Id);

        Assert.Equal(created.Id, result.Id);
        Assert.Equal("Хороший товар", result.Title);
        Assert.Equal(4, result.Rating);
        Assert.Equal(ReviewStatus.Pending, result.Status);
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
        var result = await Sut.CreateAsync(new CreateReviewRequest { Title = "Супер", Body = "Лучший товар", Rating = 5 });

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Супер", result.Title);
        Assert.Equal(5, result.Rating);
        Assert.Equal(ReviewStatus.Pending, result.Status);
        Assert.True(result.CreatedAt > DateTime.UtcNow.AddSeconds(-5));
    }

    /// <summary>
    /// Создаёт несколько отзывов, проверяет GetAll и GetById каждого.
    /// </summary>
    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task CreateMultiple_GetAll_GetByIdEach_ReturnsConsistentData(int _)
    {
        var a = await Sut.CreateAsync(new CreateReviewRequest { Title = "Отлично", Body = "Всё понравилось", Rating = 5 });
        var b = await Sut.CreateAsync(new CreateReviewRequest { Title = "Хорошо", Body = "В целом ок", Rating = 4 });
        var c = await Sut.CreateAsync(new CreateReviewRequest { Title = "Средне", Body = "Бывало лучше", Rating = 3 });

        var all = await Sut.GetAllAsync();
        Assert.Equal(3, all.Count);
        Assert.Equal("Отлично", (await Sut.GetByIdAsync(a.Id)).Title);
        Assert.Equal("Хорошо", (await Sut.GetByIdAsync(b.Id)).Title);
        Assert.Equal("Средне", (await Sut.GetByIdAsync(c.Id)).Title);

        // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
        for (var i = 0; i < 4; i++)
        {
            var extra = await Sut.CreateAsync(new CreateReviewRequest { Title = $"Отзыв {i}", Body = "Текст", Rating = 3 + i % 3 });
            await Sut.GetByIdAsync(extra.Id);
        }
        await Sut.GetAllAsync();
    }

    /// <summary>
    /// Создаёт два отзыва, один одобряет, второй отклоняет, проверяет статусы, удаляет первый.
    /// </summary>
    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task CreateApproveReject_ThenDelete_LifecycleCorrect(int _)
    {
        var toApprove = await Sut.CreateAsync(new CreateReviewRequest { Title = "Одобрить", Body = "Хороший отзыв", Rating = 5 });
        var toReject = await Sut.CreateAsync(new CreateReviewRequest { Title = "Отклонить", Body = "Плохой отзыв", Rating = 1 });

        var approved = await Sut.ApproveAsync(toApprove.Id);
        Assert.Equal(ReviewStatus.Approved, approved.Status);

        var rejected = await Sut.RejectAsync(toReject.Id);
        Assert.Equal(ReviewStatus.Rejected, rejected.Status);

        var fetchedApproved = await Sut.GetByIdAsync(toApprove.Id);
        Assert.Equal(ReviewStatus.Approved, fetchedApproved.Status);

        await Sut.DeleteAsync(toApprove.Id);
        await Assert.ThrowsAsync<NotFoundException>(() => Sut.GetByIdAsync(toApprove.Id));

        // benchmark: искусственное увеличение продолжительности теста и объёма работы с БД
        for (var i = 0; i < 4; i++)
        {
            var extra = await Sut.CreateAsync(new CreateReviewRequest { Title = $"Доп {i}", Body = "Текст", Rating = 4 });
            await Sut.ApproveAsync(extra.Id);
            await Sut.GetByIdAsync(extra.Id);
        }
        await Sut.GetAllAsync();
    }
}
