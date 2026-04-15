namespace FastIntegrationTests.Application.Entities;

/// <summary>
/// Заказ покупателя.
/// </summary>
public class Order
{
    /// <summary>Уникальный идентификатор заказа.</summary>
    public int Id { get; set; }

    /// <summary>Дата и время создания заказа (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Текущий статус заказа.</summary>
    public OrderStatus Status { get; set; }

    /// <summary>
    /// Итоговая сумма заказа.
    /// Рассчитывается в момент создания заказа и не меняется при изменении цен товаров.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Позиции заказа.</summary>
    public List<OrderItem> Items { get; set; } = new();
}
