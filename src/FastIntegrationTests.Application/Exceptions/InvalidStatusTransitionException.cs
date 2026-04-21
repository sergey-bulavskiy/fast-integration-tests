namespace FastIntegrationTests.Application.Exceptions;

/// <summary>
/// Выбрасывается при попытке выполнить недопустимый переход статуса
/// для Customer, Review или Supplier.
/// </summary>
public class InvalidStatusTransitionException : Exception
{
    /// <summary>
    /// Создаёт новый экземпляр <see cref="InvalidStatusTransitionException"/>.
    /// </summary>
    /// <param name="currentStatus">Текущий статус.</param>
    /// <param name="targetStatus">Запрашиваемый статус.</param>
    public InvalidStatusTransitionException(Enum currentStatus, Enum targetStatus)
        : base($"Переход из статуса '{currentStatus}' в статус '{targetStatus}' недопустим.")
    {
    }
}
