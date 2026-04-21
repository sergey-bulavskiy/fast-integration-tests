namespace FastIntegrationTests.Application.Services;

/// <summary>Сервис для управления категориями товаров.</summary>
public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _repository;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="CategoryService"/>.
    /// </summary>
    /// <param name="repository">Репозиторий категорий.</param>
    public CategoryService(ICategoryRepository repository) => _repository = repository;

    /// <summary>Возвращает список всех категорий.</summary>
    /// <param name="ct">Токен отмены операции.</param>
    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct = default)
    {
        var items = await _repository.GetAllAsync(ct);
        return items.Select(MapToDto).ToList();
    }

    /// <summary>Возвращает категорию по идентификатору.</summary>
    /// <param name="id">Идентификатор категории.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если категория не найдена.</exception>
    public async Task<CategoryDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Category), id);
        return MapToDto(item);
    }

    /// <summary>Создаёт новую категорию.</summary>
    /// <param name="request">Данные новой категории.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="DuplicateValueException">Если категория с таким именем уже существует.</exception>
    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default)
    {
        if (await _repository.ExistsByNameAsync(request.Name, ct))
            throw new DuplicateValueException(nameof(Category), nameof(Category.Name), request.Name);

        var item = new Category
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
        };
        var created = await _repository.AddAsync(item, ct);
        return MapToDto(created);
    }

    /// <summary>Обновляет существующую категорию.</summary>
    /// <param name="id">Идентификатор категории.</param>
    /// <param name="request">Новые данные.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если категория не найдена.</exception>
    /// <exception cref="DuplicateValueException">Если новое имя уже занято другой категорией.</exception>
    public async Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Category), id);

        if (item.Name != request.Name && await _repository.ExistsByNameAsync(request.Name, ct))
            throw new DuplicateValueException(nameof(Category), nameof(Category.Name), request.Name);

        item.Name = request.Name;
        item.Description = request.Description;
        await _repository.UpdateAsync(item, ct);
        return MapToDto(item);
    }

    /// <summary>Удаляет категорию.</summary>
    /// <param name="id">Идентификатор категории.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если категория не найдена.</exception>
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Category), id);
        await _repository.DeleteAsync(item, ct);
    }

    private static CategoryDto MapToDto(Category c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Description = c.Description,
        CreatedAt = c.CreatedAt,
    };
}
