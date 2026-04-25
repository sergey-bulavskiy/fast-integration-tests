namespace FastIntegrationTests.Tests.Testcontainers.Reviews;

/// <summary>
/// Тесты сервисного уровня: Create (ошибки), Delete, Approve, Reject для ReviewService.
/// Каждый тест создаёт изолированную БД в реальном контейнере PostgreSQL.
/// </summary>
public class ReviewServiceUdContainerTests : IAsyncLifetime, IClassFixture<ContainerFixture>
{
    private readonly ContainerFixture _fixture;
    private ShopDbContext _context = null!;
    private IReviewService Sut = null!;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="ReviewServiceUdContainerTests"/>.
    /// </summary>
    /// <param name="fixture">Запущенный контейнер с СУБД.</param>
    public ReviewServiceUdContainerTests(ContainerFixture fixture) => _fixture = fixture;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        _context = await new TestDbFactory(_fixture).CreateAsync();
        Sut = new ReviewService(new ReviewRepository(_context));
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
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
