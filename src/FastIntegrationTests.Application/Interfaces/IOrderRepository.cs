namespace FastIntegrationTests.Application.Interfaces;

/// <summary>
/// Репозиторий для работы с заказами.
/// </summary>
public interface IOrderRepository
{
    /// <summary>Возвращает все заказы (без позиций).</summary>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Список всех заказов.</returns>
    Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Возвращает заказ вместе с позициями или <c>null</c>.</summary>
    /// <param name="id">Идентификатор заказа.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Заказ с позициями или <c>null</c>, если заказ не существует.</returns>
    Task<Order?> GetByIdWithItemsAsync(int id, CancellationToken ct = default);

    /// <summary>Добавляет новый заказ и возвращает его с присвоенным Id.</summary>
    /// <param name="order">Заказ для добавления.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Добавленный заказ с присвоенным идентификатором.</returns>
    Task<Order> AddAsync(Order order, CancellationToken ct = default);

    /// <summary>Обновляет существующий заказ.</summary>
    /// <param name="order">Заказ с обновлёнными данными.</param>
    /// <param name="ct">Токен отмены операции.</param>
    Task UpdateAsync(Order order, CancellationToken ct = default);
}
