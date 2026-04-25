namespace FastIntegrationTests.Tests.IntegreSQL.Reviews;

/// <summary>
/// Тесты сервисного уровня: Create (ошибки), Delete, Approve, Reject для ReviewService.
/// Каждый тест получает клон шаблонной БД IntegreSQL (~5 мс).
/// </summary>
public class ReviewServiceUdTests : AppServiceTestBase
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
    public async Task CreateAsync_WhenInvalidRating_ThrowsInvalidRatingException(int _)
    {
        await Assert.ThrowsAsync<InvalidRatingException>(
            () => Sut.CreateAsync(new CreateReviewRequest { Title = "Плохо", Body = "Не понравилось", Rating = 6 }));
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task DeleteAsync_RemovesReview(int _)
    {
        var created = await Sut.CreateAsync(new CreateReviewRequest { Title = "Удаляемый", Body = "Текст", Rating = 3 });

        await Sut.DeleteAsync(created.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => Sut.GetByIdAsync(created.Id));
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task ApproveAsync_ApprovesReview(int _)
    {
        var created = await Sut.CreateAsync(new CreateReviewRequest { Title = "На модерации", Body = "Текст", Rating = 4 });

        var approved = await Sut.ApproveAsync(created.Id);

        Assert.Equal(ReviewStatus.Approved, approved.Status);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task ApproveAsync_WhenNotPending_ThrowsInvalidStatusTransitionException(int _)
    {
        var created = await Sut.CreateAsync(new CreateReviewRequest { Title = "Одобренный", Body = "Текст", Rating = 4 });
        await Sut.ApproveAsync(created.Id);

        await Assert.ThrowsAsync<InvalidStatusTransitionException>(() => Sut.ApproveAsync(created.Id));
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task RejectAsync_RejectsReview(int _)
    {
        var created = await Sut.CreateAsync(new CreateReviewRequest { Title = "Спам", Body = "Текст", Rating = 1 });

        var rejected = await Sut.RejectAsync(created.Id);

        Assert.Equal(ReviewStatus.Rejected, rejected.Status);
    }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public async Task RejectAsync_WhenNotPending_ThrowsInvalidStatusTransitionException(int _)
    {
        var created = await Sut.CreateAsync(new CreateReviewRequest { Title = "Отклонённый", Body = "Текст", Rating = 1 });
        await Sut.RejectAsync(created.Id);

        await Assert.ThrowsAsync<InvalidStatusTransitionException>(() => Sut.RejectAsync(created.Id));
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
