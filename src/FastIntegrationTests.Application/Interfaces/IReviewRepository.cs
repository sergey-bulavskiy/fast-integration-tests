namespace FastIntegrationTests.Application.Interfaces;

/// <summary>Репозиторий отзывов.</summary>
public interface IReviewRepository
{
    /// <summary>Возвращает все отзывы.</summary>
    /// <param name="ct">Токен отмены операции.</param>
    Task<IReadOnlyList<Review>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Возвращает отзыв по идентификатору или null.</summary>
    /// <param name="id">Идентификатор отзыва.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<Review?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Добавляет новый отзыв и возвращает его.</summary>
    /// <param name="review">Сущность отзыва.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<Review> AddAsync(Review review, CancellationToken ct = default);

    /// <summary>Обновляет существующий отзыв.</summary>
    /// <param name="review">Сущность отзыва.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task UpdateAsync(Review review, CancellationToken ct = default);

    /// <summary>Удаляет отзыв.</summary>
    /// <param name="review">Сущность отзыва.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task DeleteAsync(Review review, CancellationToken ct = default);
}
