namespace FastIntegrationTests.Tests.Testcontainers.Reviews;

/// <summary>
/// Тесты сервисного уровня: GetAll, GetById, Create для ReviewService.
/// Каждый тест создаёт изолированную БД в реальном контейнере PostgreSQL.
/// </summary>
public class ReviewServiceCrContainerTests : IAsyncLifetime, IClassFixture<ContainerFixture>
{
    private readonly ContainerFixture _fixture;
    private ShopDbContext _context = null!;
    private IReviewService Sut = null!;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="ReviewServiceCrContainerTests"/>.
    /// </summary>
    /// <param name="fixture">Запущенный контейнер с СУБД.</param>
    public ReviewServiceCrContainerTests(ContainerFixture fixture) => _fixture = fixture;

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
}
