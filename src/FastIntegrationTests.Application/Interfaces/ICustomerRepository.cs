namespace FastIntegrationTests.Application.Interfaces;

/// <summary>Репозиторий покупателей.</summary>
public interface ICustomerRepository
{
    /// <summary>Возвращает всех покупателей.</summary>
    /// <param name="ct">Токен отмены операции.</param>
    Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Возвращает покупателя по идентификатору или null.</summary>
    /// <param name="id">Идентификатор покупателя.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Проверяет, существует ли покупатель с указанным email.</summary>
    /// <param name="email">Email для проверки.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>Добавляет нового покупателя и возвращает его.</summary>
    /// <param name="customer">Сущность покупателя.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task<Customer> AddAsync(Customer customer, CancellationToken ct = default);

    /// <summary>Обновляет существующего покупателя.</summary>
    /// <param name="customer">Сущность покупателя.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task UpdateAsync(Customer customer, CancellationToken ct = default);

    /// <summary>Удаляет покупателя.</summary>
    /// <param name="customer">Сущность покупателя.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task DeleteAsync(Customer customer, CancellationToken ct = default);
}
