namespace FastIntegrationTests.Application.Interfaces;

/// <summary>Сервис для управления категориями товаров.</summary>
public interface ICategoryService
{
    /// <summary>Возвращает список всех категорий.</summary>
    /// <param name="ct">Токен отмены операции.</param>
    Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Возвращает категорию по идентификатору.</summary>
    /// <param name="id">Идентификатор категории.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<CategoryDto> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Создаёт новую категорию.</summary>
    /// <param name="request">Данные новой категории.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default);

    /// <summary>Обновляет существующую категорию.</summary>
    /// <param name="id">Идентификатор категории.</param>
    /// <param name="request">Новые данные.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken ct = default);

    /// <summary>Удаляет категорию.</summary>
    /// <param name="id">Идентификатор категории.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
