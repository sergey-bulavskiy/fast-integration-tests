namespace FastIntegrationTests.Application.Services;

/// <summary>Сервис для управления отзывами.</summary>
public class ReviewService : IReviewService
{
    private readonly IReviewRepository _repository;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="ReviewService"/>.
    /// </summary>
    /// <param name="repository">Репозиторий отзывов.</param>
    public ReviewService(IReviewRepository repository) => _repository = repository;

    /// <summary>Возвращает список всех отзывов.</summary>
    /// <param name="ct">Токен отмены операции.</param>
    public async Task<IReadOnlyList<ReviewDto>> GetAllAsync(CancellationToken ct = default)
    {
        var items = await _repository.GetAllAsync(ct);
        return items.Select(MapToDto).ToList();
    }

    /// <summary>Возвращает отзыв по идентификатору.</summary>
    /// <param name="id">Идентификатор отзыва.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если отзыв не найден.</exception>
    public async Task<ReviewDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Review), id);
        return MapToDto(item);
    }

    /// <summary>Создаёт новый отзыв.</summary>
    /// <param name="request">Данные нового отзыва.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="InvalidRatingException">Если рейтинг вне диапазона 1–5.</exception>
    public async Task<ReviewDto> CreateAsync(CreateReviewRequest request, CancellationToken ct = default)
    {
        if (request.Rating < 1 || request.Rating > 5)
            throw new InvalidRatingException(request.Rating);

        var item = new Review
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Body = request.Body,
            Rating = request.Rating,
            Status = ReviewStatus.Pending,
            CreatedAt = DateTime.UtcNow,
        };
        var created = await _repository.AddAsync(item, ct);
        return MapToDto(created);
    }

    /// <summary>Удаляет отзыв.</summary>
    /// <param name="id">Идентификатор отзыва.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если отзыв не найден.</exception>
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Review), id);
        await _repository.DeleteAsync(item, ct);
    }

    /// <summary>Одобряет отзыв (Pending → Approved).</summary>
    /// <param name="id">Идентификатор отзыва.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если отзыв не найден.</exception>
    /// <exception cref="InvalidStatusTransitionException">Если статус не Pending.</exception>
    public async Task<ReviewDto> ApproveAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Review), id);

        if (item.Status != ReviewStatus.Pending)
            throw new InvalidStatusTransitionException(item.Status, ReviewStatus.Approved);

        item.Status = ReviewStatus.Approved;
        await _repository.UpdateAsync(item, ct);
        return MapToDto(item);
    }

    /// <summary>Отклоняет отзыв (Pending → Rejected).</summary>
    /// <param name="id">Идентификатор отзыва.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если отзыв не найден.</exception>
    /// <exception cref="InvalidStatusTransitionException">Если статус не Pending.</exception>
    public async Task<ReviewDto> RejectAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Review), id);

        if (item.Status != ReviewStatus.Pending)
            throw new InvalidStatusTransitionException(item.Status, ReviewStatus.Rejected);

        item.Status = ReviewStatus.Rejected;
        await _repository.UpdateAsync(item, ct);
        return MapToDto(item);
    }

    private static ReviewDto MapToDto(Review r) => new()
    {
        Id = r.Id,
        Title = r.Title,
        Body = r.Body,
        Rating = r.Rating,
        Status = r.Status,
        CreatedAt = r.CreatedAt,
    };
}
