namespace FastIntegrationTests.Application.Interfaces;

/// <summary>Сервис для управления покупателями.</summary>
public interface ICustomerService
{
    /// <summary>Возвращает список всех покупателей.</summary>
    /// <param name="ct">Токен отмены операции.</param>
    Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Возвращает покупателя по идентификатору.</summary>
    /// <param name="id">Идентификатор покупателя.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<CustomerDto> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Создаёт нового покупателя.</summary>
    /// <param name="request">Данные нового покупателя.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default);

    /// <summary>Обновляет существующего покупателя.</summary>
    /// <param name="id">Идентификатор покупателя.</param>
    /// <param name="request">Новые данные.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<CustomerDto> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken ct = default);

    /// <summary>Удаляет покупателя.</summary>
    /// <param name="id">Идентификатор покупателя.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Блокирует покупателя.</summary>
    /// <param name="id">Идентификатор покупателя.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<CustomerDto> BanAsync(Guid id, CancellationToken ct = default);

    /// <summary>Активирует покупателя.</summary>
    /// <param name="id">Идентификатор покупателя.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<CustomerDto> ActivateAsync(Guid id, CancellationToken ct = default);

    /// <summary>Деактивирует покупателя.</summary>
    /// <param name="id">Идентификатор покупателя.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<CustomerDto> DeactivateAsync(Guid id, CancellationToken ct = default);
}
