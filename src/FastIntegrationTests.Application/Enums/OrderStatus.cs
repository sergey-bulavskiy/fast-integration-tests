namespace FastIntegrationTests.Application.Enums;

/// <summary>
/// Статус заказа. Определяет текущее состояние заказа в жизненном цикле.
/// </summary>
public enum OrderStatus
{
    /// <summary>Заказ создан, ожидает подтверждения.</summary>
    New = 0,

    /// <summary>Заказ подтверждён менеджером.</summary>
    Confirmed = 1,

    /// <summary>Заказ передан в доставку.</summary>
    Shipped = 2,

    /// <summary>Заказ доставлен покупателю. Финальный статус.</summary>
    Completed = 3,

    /// <summary>Заказ отменён. Финальный статус.</summary>
    Cancelled = 4,
}
