namespace FastIntegrationTests.Application.Interfaces;

/// <summary>
/// Репозиторий для работы с товарами.
/// </summary>
public interface IProductRepository
{
    /// <summary>Возвращает все товары.</summary>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Список всех товаров.</returns>
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Возвращает товар по идентификатору или <c>null</c>.</summary>
    /// <param name="id">Идентификатор товара.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Найденный товар или <c>null</c>, если товар не существует.</returns>
    Task<Product?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>Добавляет новый товар и возвращает его с присвоенным Id.</summary>
    /// <param name="product">Товар для добавления.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Добавленный товар с присвоенным идентификатором.</returns>
    Task<Product> AddAsync(Product product, CancellationToken ct = default);

    /// <summary>Обновляет существующий товар.</summary>
    /// <param name="product">Товар с обновлёнными данными.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task UpdateAsync(Product product, CancellationToken ct = default);

    /// <summary>Удаляет товар.</summary>
    /// <param name="product">Товар для удаления.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task DeleteAsync(Product product, CancellationToken ct = default);
}
