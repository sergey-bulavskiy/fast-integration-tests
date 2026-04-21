namespace FastIntegrationTests.Application.Interfaces;

/// <summary>Репозиторий категорий.</summary>
public interface ICategoryRepository
{
    /// <summary>Возвращает все категории.</summary>
    /// <param name="ct">Токен отмены операции.</param>
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Возвращает категорию по идентификатору или null.</summary>
    /// <param name="id">Идентификатор категории.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Проверяет, существует ли категория с указанным именем.</summary>
    /// <param name="name">Название для проверки.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);

    /// <summary>Добавляет новую категорию и возвращает её.</summary>
    /// <param name="category">Сущность категории.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<Category> AddAsync(Category category, CancellationToken ct = default);

    /// <summary>Обновляет существующую категорию.</summary>
    /// <param name="category">Сущность категории.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task UpdateAsync(Category category, CancellationToken ct = default);

    /// <summary>Удаляет категорию.</summary>
    /// <param name="category">Сущность категории.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task DeleteAsync(Category category, CancellationToken ct = default);
}
