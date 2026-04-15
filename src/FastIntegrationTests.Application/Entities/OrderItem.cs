namespace FastIntegrationTests.Application.Entities;

/// <summary>
/// Позиция заказа — конкретный товар с зафиксированной ценой и количеством.
/// </summary>
public class OrderItem
{
    /// <summary>Уникальный идентификатор позиции.</summary>
    public int Id { get; set; }

    /// <summary>Идентификатор заказа.</summary>
    public int OrderId { get; set; }

    /// <summary>Навигационное свойство — заказ.</summary>
    public Order Order { get; set; } = null!;

    /// <summary>Идентификатор товара.</summary>
    public int ProductId { get; set; }

    /// <summary>Навигационное свойство — товар.</summary>
    public Product Product { get; set; } = null!;

    /// <summary>Количество единиц товара.</summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Цена товара на момент оформления заказа.
    /// Фиксируется при создании заказа и не изменяется при последующем изменении цены товара.
    /// </summary>
    public decimal UnitPrice { get; set; }
}
