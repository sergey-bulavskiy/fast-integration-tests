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
}
