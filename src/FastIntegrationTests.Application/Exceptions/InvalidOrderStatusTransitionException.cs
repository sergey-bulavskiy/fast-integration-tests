namespace FastIntegrationTests.Application.Exceptions;

/// <summary>
/// Выбрасывается при попытке выполнить недопустимый переход статуса заказа.
/// </summary>
public class InvalidOrderStatusTransitionException : Exception
{
    /// <summary>
    /// Создаёт новый экземпляр <see cref="InvalidOrderStatusTransitionException"/>.
    /// </summary>
    /// <param name="currentStatus">Текущий статус заказа.</param>
    /// <param name="targetStatus">Запрашиваемый целевой статус.</param>
    public InvalidOrderStatusTransitionException(OrderStatus currentStatus, OrderStatus targetStatus)
        : base($"Переход из статуса '{currentStatus}' в статус '{targetStatus}' недопустим.")
    {
    }
}
