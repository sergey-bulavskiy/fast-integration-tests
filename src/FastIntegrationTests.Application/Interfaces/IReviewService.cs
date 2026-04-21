namespace FastIntegrationTests.Application.Interfaces;

/// <summary>Сервис для управления отзывами.</summary>
public interface IReviewService
{
    /// <summary>Возвращает список всех отзывов.</summary>
    /// <param name="ct">Токен отмены операции.</param>
    Task<IReadOnlyList<ReviewDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Возвращает отзыв по идентификатору.</summary>
    /// <param name="id">Идентификатор отзыва.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<ReviewDto> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Создаёт новый отзыв.</summary>
    /// <param name="request">Данные нового отзыва.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<ReviewDto> CreateAsync(CreateReviewRequest request, CancellationToken ct = default);

    /// <summary>Удаляет отзыв.</summary>
    /// <param name="id">Идентификатор отзыва.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Одобряет отзыв (Pending → Approved).</summary>
    /// <param name="id">Идентификатор отзыва.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<ReviewDto> ApproveAsync(Guid id, CancellationToken ct = default);

    /// <summary>Отклоняет отзыв (Pending → Rejected).</summary>
    /// <param name="id">Идентификатор отзыва.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<ReviewDto> RejectAsync(Guid id, CancellationToken ct = default);
}
