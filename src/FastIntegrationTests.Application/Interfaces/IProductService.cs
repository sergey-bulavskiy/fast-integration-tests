namespace FastIntegrationTests.Application.Interfaces;

/// <summary>
/// Интерфейс сервиса управления товарами каталога.
/// </summary>
public interface IProductService
{
    /// <summary>
    /// Возвращает список всех товаров.
    /// </summary>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Список DTO товаров.</returns>
    Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Возвращает товар по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор товара.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>DTO товара.</returns>
    /// <exception cref="NotFoundException">Если товар не найден.</exception>
    Task<ProductDto> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Создаёт новый товар.
    /// </summary>
    /// <param name="request">Данные нового товара.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>DTO созданного товара.</returns>
    Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken ct = default);

    /// <summary>
    /// Обновляет существующий товар.
    /// </summary>
    /// <param name="id">Идентификатор товара.</param>
    /// <param name="request">Новые данные товара.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>DTO обновлённого товара.</returns>
    /// <exception cref="NotFoundException">Если товар не найден.</exception>
    Task<ProductDto> UpdateAsync(int id, UpdateProductRequest request, CancellationToken ct = default);

    /// <summary>
    /// Удаляет товар по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор товара.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если товар не найден.</exception>
    Task DeleteAsync(int id, CancellationToken ct = default);
}
