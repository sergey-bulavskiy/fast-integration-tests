namespace FastIntegrationTests.Application.Interfaces;

/// <summary>
/// Репозиторий для работы с заказами.
/// </summary>
public interface IOrderRepository
{
    /// <summary>Возвращает все заказы (без позиций).</summary>
    Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Возвращает заказ вместе с позициями или <c>null</c>.</summary>
    Task<Order?> GetByIdWithItemsAsync(int id, CancellationToken ct = default);

    /// <summary>Добавляет новый заказ и возвращает его с присвоенным Id.</summary>
    Task<Order> AddAsync(Order order, CancellationToken ct = default);

    /// <summary>Обновляет существующий заказ.</summary>
    Task UpdateAsync(Order order, CancellationToken ct = default);
}
