namespace FastIntegrationTests.Application.Interfaces;

/// <summary>Репозиторий поставщиков.</summary>
public interface ISupplierRepository
{
    /// <summary>Возвращает всех поставщиков.</summary>
    /// <param name="ct">Токен отмены операции.</param>
    Task<IReadOnlyList<Supplier>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Возвращает поставщика по идентификатору или null.</summary>
    /// <param name="id">Идентификатор поставщика.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<Supplier?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Проверяет, существует ли поставщик с указанным contactEmail.</summary>
    /// <param name="email">Email для проверки.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>Добавляет нового поставщика и возвращает его.</summary>
    /// <param name="supplier">Сущность поставщика.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<Supplier> AddAsync(Supplier supplier, CancellationToken ct = default);

    /// <summary>Обновляет существующего поставщика.</summary>
    /// <param name="supplier">Сущность поставщика.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task UpdateAsync(Supplier supplier, CancellationToken ct = default);

    /// <summary>Удаляет поставщика.</summary>
    /// <param name="supplier">Сущность поставщика.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task DeleteAsync(Supplier supplier, CancellationToken ct = default);
}
