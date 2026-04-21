namespace FastIntegrationTests.Application.Interfaces;

/// <summary>Сервис для управления скидками.</summary>
public interface IDiscountService
{
    /// <summary>Возвращает список всех скидок.</summary>
    /// <param name="ct">Токен отмены операции.</param>
    Task<IReadOnlyList<DiscountDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Возвращает скидку по идентификатору.</summary>
    /// <param name="id">Идентификатор скидки.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<DiscountDto> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Создаёт новую скидку.</summary>
    /// <param name="request">Данные новой скидки.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<DiscountDto> CreateAsync(CreateDiscountRequest request, CancellationToken ct = default);

    /// <summary>Обновляет существующую скидку.</summary>
    /// <param name="id">Идентификатор скидки.</param>
    /// <param name="request">Новые данные.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<DiscountDto> UpdateAsync(Guid id, UpdateDiscountRequest request, CancellationToken ct = default);

    /// <summary>Удаляет скидку.</summary>
    /// <param name="id">Идентификатор скидки.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Активирует скидку.</summary>
    /// <param name="id">Идентификатор скидки.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<DiscountDto> ActivateAsync(Guid id, CancellationToken ct = default);

    /// <summary>Деактивирует скидку.</summary>
    /// <param name="id">Идентификатор скидки.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<DiscountDto> DeactivateAsync(Guid id, CancellationToken ct = default);
}
