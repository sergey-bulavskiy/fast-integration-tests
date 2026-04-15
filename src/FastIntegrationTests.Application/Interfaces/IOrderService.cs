namespace FastIntegrationTests.Application.Interfaces;

/// <summary>
/// Интерфейс сервиса управления заказами и их жизненным циклом.
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// Возвращает список всех заказов.
    /// </summary>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Список DTO заказов.</returns>
    Task<IReadOnlyList<OrderDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Возвращает заказ с позициями по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор заказа.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>DTO заказа с позициями.</returns>
    /// <exception cref="NotFoundException">Если заказ не найден.</exception>
    Task<OrderDto> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Создаёт новый заказ. Фиксирует цены товаров и рассчитывает итоговую сумму.
    /// </summary>
    /// <param name="request">Данные нового заказа с позициями.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>DTO созданного заказа.</returns>
    /// <exception cref="NotFoundException">Если один из указанных товаров не найден.</exception>
    Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken ct = default);

    /// <summary>
    /// Подтверждает заказ (New → Confirmed).
    /// </summary>
    /// <param name="id">Идентификатор заказа.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>DTO обновлённого заказа.</returns>
    /// <exception cref="NotFoundException">Если заказ не найден.</exception>
    /// <exception cref="InvalidOrderStatusTransitionException">Если переход недопустим.</exception>
    Task<OrderDto> ConfirmAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Передаёт заказ в доставку (Confirmed → Shipped).
    /// </summary>
    /// <param name="id">Идентификатор заказа.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>DTO обновлённого заказа.</returns>
    /// <exception cref="NotFoundException">Если заказ не найден.</exception>
    /// <exception cref="InvalidOrderStatusTransitionException">Если переход недопустим.</exception>
    Task<OrderDto> ShipAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Завершает заказ (Shipped → Completed).
    /// </summary>
    /// <param name="id">Идентификатор заказа.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>DTO обновлённого заказа.</returns>
    /// <exception cref="NotFoundException">Если заказ не найден.</exception>
    /// <exception cref="InvalidOrderStatusTransitionException">Если переход недопустим.</exception>
    Task<OrderDto> CompleteAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Отменяет заказ (New/Confirmed → Cancelled).
    /// </summary>
    /// <param name="id">Идентификатор заказа.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>DTO обновлённого заказа.</returns>
    /// <exception cref="NotFoundException">Если заказ не найден.</exception>
    /// <exception cref="InvalidOrderStatusTransitionException">Если переход недопустим.</exception>
    Task<OrderDto> CancelAsync(int id, CancellationToken ct = default);
}
