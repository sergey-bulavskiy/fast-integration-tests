namespace FastIntegrationTests.Application.Services;

/// <summary>Сервис для управления скидками.</summary>
public class DiscountService : IDiscountService
{
    private readonly IDiscountRepository _repository;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="DiscountService"/>.
    /// </summary>
    /// <param name="repository">Репозиторий скидок.</param>
    public DiscountService(IDiscountRepository repository) => _repository = repository;

    /// <summary>Возвращает список всех скидок.</summary>
    /// <param name="ct">Токен отмены операции.</param>
    public async Task<IReadOnlyList<DiscountDto>> GetAllAsync(CancellationToken ct = default)
    {
        var items = await _repository.GetAllAsync(ct);
        return items.Select(MapToDto).ToList();
    }

    /// <summary>Возвращает скидку по идентификатору.</summary>
    /// <param name="id">Идентификатор скидки.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если скидка не найдена.</exception>
    public async Task<DiscountDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Discount), id);
        return MapToDto(item);
    }

    /// <summary>Создаёт новую скидку.</summary>
    /// <param name="request">Данные новой скидки.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="InvalidDiscountPercentException">Если процент вне диапазона 1–100.</exception>
    /// <exception cref="DuplicateValueException">Если скидка с таким кодом уже существует.</exception>
    public async Task<DiscountDto> CreateAsync(CreateDiscountRequest request, CancellationToken ct = default)
    {
        if (request.DiscountPercent < 1 || request.DiscountPercent > 100)
            throw new InvalidDiscountPercentException(request.DiscountPercent);

        if (await _repository.ExistsByCodeAsync(request.Code, ct))
            throw new DuplicateValueException(nameof(Discount), nameof(Discount.Code), request.Code);

        var item = new Discount
        {
            Id = Guid.NewGuid(),
            Code = request.Code,
            DiscountPercent = request.DiscountPercent,
            IsActive = false,
            ExpiresAt = request.ExpiresAt,
            CreatedAt = DateTime.UtcNow,
        };
        var created = await _repository.AddAsync(item, ct);
        return MapToDto(created);
    }

    /// <summary>Обновляет существующую скидку.</summary>
    /// <param name="id">Идентификатор скидки.</param>
    /// <param name="request">Новые данные.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если скидка не найдена.</exception>
    /// <exception cref="InvalidDiscountPercentException">Если процент вне диапазона 1–100.</exception>
    /// <exception cref="DuplicateValueException">Если новый код уже занят.</exception>
    public async Task<DiscountDto> UpdateAsync(Guid id, UpdateDiscountRequest request, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Discount), id);

        if (request.DiscountPercent < 1 || request.DiscountPercent > 100)
            throw new InvalidDiscountPercentException(request.DiscountPercent);

        if (item.Code != request.Code && await _repository.ExistsByCodeAsync(request.Code, ct))
            throw new DuplicateValueException(nameof(Discount), nameof(Discount.Code), request.Code);

        item.Code = request.Code;
        item.DiscountPercent = request.DiscountPercent;
        item.ExpiresAt = request.ExpiresAt;
        await _repository.UpdateAsync(item, ct);
        return MapToDto(item);
    }

    /// <summary>Удаляет скидку.</summary>
    /// <param name="id">Идентификатор скидки.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если скидка не найдена.</exception>
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Discount), id);
        await _repository.DeleteAsync(item, ct);
    }

    /// <summary>Активирует скидку.</summary>
    /// <param name="id">Идентификатор скидки.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если скидка не найдена.</exception>
    public async Task<DiscountDto> ActivateAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Discount), id);
        item.IsActive = true;
        await _repository.UpdateAsync(item, ct);
        return MapToDto(item);
    }

    /// <summary>Деактивирует скидку.</summary>
    /// <param name="id">Идентификатор скидки.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <exception cref="NotFoundException">Если скидка не найдена.</exception>
    public async Task<DiscountDto> DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Discount), id);
        item.IsActive = false;
        await _repository.UpdateAsync(item, ct);
        return MapToDto(item);
    }

    private static DiscountDto MapToDto(Discount d) => new()
    {
        Id = d.Id,
        Code = d.Code,
        DiscountPercent = d.DiscountPercent,
        IsActive = d.IsActive,
        ExpiresAt = d.ExpiresAt,
        CreatedAt = d.CreatedAt,
    };
}
