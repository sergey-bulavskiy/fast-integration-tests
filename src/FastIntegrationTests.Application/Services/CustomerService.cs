namespace FastIntegrationTests.Application.Services;

/// <summary>Сервис для управления покупателями.</summary>
public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _repository;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="CustomerService"/>.
    /// </summary>
    /// <param name="repository">Репозиторий покупателей.</param>
    public CustomerService(ICustomerRepository repository) => _repository = repository;

    /// <summary>Возвращает список всех покупателей.</summary>
    /// <param name="ct">Токен отмены операции.</param>
    public async Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken ct = default)
    {
        var items = await _repository.GetAllAsync(ct);
        return items.Select(MapToDto).ToList();
    }

    /// <summary>Возвращает покупателя по идентификатору.</summary>
    /// <param name="id">Идентификатор покупателя.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если покупатель не найден.</exception>
    public async Task<CustomerDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Customer), id);
        return MapToDto(item);
    }

    /// <summary>Создаёт нового покупателя.</summary>
    /// <param name="request">Данные нового покупателя.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="DuplicateValueException">Если покупатель с таким email уже существует.</exception>
    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default)
    {
        if (await _repository.ExistsByEmailAsync(request.Email, ct))
            throw new DuplicateValueException(nameof(Customer), nameof(Customer.Email), request.Email);

        var item = new Customer
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            Status = CustomerStatus.Active,
            CreatedAt = DateTime.UtcNow,
        };
        var created = await _repository.AddAsync(item, ct);
        return MapToDto(created);
    }

    /// <summary>Обновляет существующего покупателя.</summary>
    /// <param name="id">Идентификатор покупателя.</param>
    /// <param name="request">Новые данные.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если покупатель не найден.</exception>
    /// <exception cref="DuplicateValueException">Если новый email уже занят.</exception>
    public async Task<CustomerDto> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Customer), id);

        if (item.Email != request.Email && await _repository.ExistsByEmailAsync(request.Email, ct))
            throw new DuplicateValueException(nameof(Customer), nameof(Customer.Email), request.Email);

        item.Name = request.Name;
        item.Email = request.Email;
        item.Phone = request.Phone;
        await _repository.UpdateAsync(item, ct);
        return MapToDto(item);
    }

    /// <summary>Удаляет покупателя.</summary>
    /// <param name="id">Идентификатор покупателя.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если покупатель не найден.</exception>
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Customer), id);
        await _repository.DeleteAsync(item, ct);
    }

    /// <summary>Блокирует покупателя.</summary>
    /// <param name="id">Идентификатор покупателя.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если покупатель не найден.</exception>
    /// <exception cref="InvalidStatusTransitionException">Если покупатель уже заблокирован.</exception>
    public async Task<CustomerDto> BanAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Customer), id);

        if (item.Status == CustomerStatus.Banned)
            throw new InvalidStatusTransitionException(item.Status, CustomerStatus.Banned);

        item.Status = CustomerStatus.Banned;
        await _repository.UpdateAsync(item, ct);
        return MapToDto(item);
    }

    /// <summary>Активирует покупателя.</summary>
    /// <param name="id">Идентификатор покупателя.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если покупатель не найден.</exception>
    public async Task<CustomerDto> ActivateAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Customer), id);

        item.Status = CustomerStatus.Active;
        await _repository.UpdateAsync(item, ct);
        return MapToDto(item);
    }

    /// <summary>Деактивирует покупателя.</summary>
    /// <param name="id">Идентификатор покупателя.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если покупатель не найден.</exception>
    public async Task<CustomerDto> DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Customer), id);

        item.Status = CustomerStatus.Inactive;
        await _repository.UpdateAsync(item, ct);
        return MapToDto(item);
    }

    private static CustomerDto MapToDto(Customer c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Email = c.Email,
        Phone = c.Phone,
        Status = c.Status,
        CreatedAt = c.CreatedAt,
    };
}
