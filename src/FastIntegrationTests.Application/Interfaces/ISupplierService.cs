namespace FastIntegrationTests.Application.Interfaces;

/// <summary>Сервис для управления поставщиками.</summary>
public interface ISupplierService
{
    /// <summary>Возвращает список всех поставщиков.</summary>
    /// <param name="ct">Токен отмены операции.</param>
    Task<IReadOnlyList<SupplierDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Возвращает поставщика по идентификатору.</summary>
    /// <param name="id">Идентификатор поставщика.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<SupplierDto> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Создаёт нового поставщика.</summary>
    /// <param name="request">Данные нового поставщика.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<SupplierDto> CreateAsync(CreateSupplierRequest request, CancellationToken ct = default);

    /// <summary>Обновляет существующего поставщика.</summary>
    /// <param name="id">Идентификатор поставщика.</param>
    /// <param name="request">Новые данные.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<SupplierDto> UpdateAsync(Guid id, UpdateSupplierRequest request, CancellationToken ct = default);

    /// <summary>Удаляет поставщика.</summary>
    /// <param name="id">Идентификатор поставщика.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Активирует поставщика.</summary>
    /// <param name="id">Идентификатор поставщика.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<SupplierDto> ActivateAsync(Guid id, CancellationToken ct = default);

    /// <summary>Деактивирует поставщика.</summary>
    /// <param name="id">Идентификатор поставщика.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<SupplierDto> DeactivateAsync(Guid id, CancellationToken ct = default);
}
