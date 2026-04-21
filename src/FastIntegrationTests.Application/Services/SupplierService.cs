namespace FastIntegrationTests.Application.Services;

/// <summary>Сервис для управления поставщиками.</summary>
public class SupplierService : ISupplierService
{
    private readonly ISupplierRepository _repository;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="SupplierService"/>.
    /// </summary>
    /// <param name="repository">Репозиторий поставщиков.</param>
    public SupplierService(ISupplierRepository repository) => _repository = repository;

    /// <summary>Возвращает список всех поставщиков.</summary>
    /// <param name="ct">Токен отмены операции.</param>
    public async Task<IReadOnlyList<SupplierDto>> GetAllAsync(CancellationToken ct = default)
    {
        var items = await _repository.GetAllAsync(ct);
        return items.Select(MapToDto).ToList();
    }

    /// <summary>Возвращает поставщика по идентификатору.</summary>
    /// <param name="id">Идентификатор поставщика.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если поставщик не найден.</exception>
    public async Task<SupplierDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Supplier), id);
        return MapToDto(item);
    }

    /// <summary>Создаёт нового поставщика.</summary>
    /// <param name="request">Данные нового поставщика.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="DuplicateValueException">Если поставщик с таким email уже существует.</exception>
    public async Task<SupplierDto> CreateAsync(CreateSupplierRequest request, CancellationToken ct = default)
    {
        if (await _repository.ExistsByEmailAsync(request.ContactEmail, ct))
            throw new DuplicateValueException(nameof(Supplier), nameof(Supplier.ContactEmail), request.ContactEmail);

        var item = new Supplier
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            ContactEmail = request.ContactEmail,
            Country = request.Country,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        var created = await _repository.AddAsync(item, ct);
        return MapToDto(created);
    }

    /// <summary>Обновляет существующего поставщика.</summary>
    /// <param name="id">Идентификатор поставщика.</param>
    /// <param name="request">Новые данные.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если поставщик не найден.</exception>
    /// <exception cref="DuplicateValueException">Если новый email уже занят.</exception>
    public async Task<SupplierDto> UpdateAsync(Guid id, UpdateSupplierRequest request, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Supplier), id);

        if (item.ContactEmail != request.ContactEmail && await _repository.ExistsByEmailAsync(request.ContactEmail, ct))
            throw new DuplicateValueException(nameof(Supplier), nameof(Supplier.ContactEmail), request.ContactEmail);

        item.Name = request.Name;
        item.ContactEmail = request.ContactEmail;
        item.Country = request.Country;
        await _repository.UpdateAsync(item, ct);
        return MapToDto(item);
    }

    /// <summary>Удаляет поставщика.</summary>
    /// <param name="id">Идентификатор поставщика.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если поставщик не найден.</exception>
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Supplier), id);
        await _repository.DeleteAsync(item, ct);
    }

    /// <summary>Активирует поставщика.</summary>
    /// <param name="id">Идентификатор поставщика.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если поставщик не найден.</exception>
    public async Task<SupplierDto> ActivateAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Supplier), id);
        item.IsActive = true;
        await _repository.UpdateAsync(item, ct);
        return MapToDto(item);
    }

    /// <summary>Деактивирует поставщика.</summary>
    /// <param name="id">Идентификатор поставщика.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если поставщик не найден.</exception>
    public async Task<SupplierDto> DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Supplier), id);
        item.IsActive = false;
        await _repository.UpdateAsync(item, ct);
        return MapToDto(item);
    }

    private static SupplierDto MapToDto(Supplier s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        ContactEmail = s.ContactEmail,
        Country = s.Country,
        IsActive = s.IsActive,
        CreatedAt = s.CreatedAt,
    };
}
