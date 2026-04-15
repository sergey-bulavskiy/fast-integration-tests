namespace FastIntegrationTests.Application.Interfaces;

/// <summary>
/// Репозиторий для работы с товарами.
/// </summary>
public interface IProductRepository
{
    /// <summary>Возвращает все товары.</summary>
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Возвращает товар по идентификатору или <c>null</c>.</summary>
    Task<Product?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>Добавляет новый товар и возвращает его с присвоенным Id.</summary>
    Task<Product> AddAsync(Product product, CancellationToken ct = default);

    /// <summary>Обновляет существующий товар.</summary>
    Task UpdateAsync(Product product, CancellationToken ct = default);

    /// <summary>Удаляет товар.</summary>
    Task DeleteAsync(Product product, CancellationToken ct = default);
}
