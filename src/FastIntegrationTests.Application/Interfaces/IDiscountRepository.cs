namespace FastIntegrationTests.Application.Interfaces;

/// <summary>Репозиторий скидок.</summary>
public interface IDiscountRepository
{
    /// <summary>Возвращает все скидки.</summary>
    /// <param name="ct">Токен отмены операции.</param>
    Task<IReadOnlyList<Discount>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Возвращает скидку по идентификатору или null.</summary>
    /// <param name="id">Идентификатор скидки.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<Discount?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Проверяет, существует ли скидка с указанным кодом.</summary>
    /// <param name="code">Код для проверки.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<bool> ExistsByCodeAsync(string code, CancellationToken ct = default);

    /// <summary>Добавляет новую скидку и возвращает её.</summary>
    /// <param name="discount">Сущность скидки.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<Discount> AddAsync(Discount discount, CancellationToken ct = default);

    /// <summary>Обновляет существующую скидку.</summary>
    /// <param name="discount">Сущность скидки.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task UpdateAsync(Discount discount, CancellationToken ct = default);

    /// <summary>Удаляет скидку.</summary>
    /// <param name="discount">Сущность скидки.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task DeleteAsync(Discount discount, CancellationToken ct = default);
}
