namespace FastIntegrationTests.Application.DTOs;

/// <summary>
/// Данные позиции заказа, возвращаемые клиенту.
/// </summary>
public class OrderItemDto
{
    /// <summary>Идентификатор позиции.</summary>
    public int Id { get; set; }

    /// <summary>Идентификатор товара.</summary>
    public int ProductId { get; set; }

    /// <summary>Количество единиц товара.</summary>
    public int Quantity { get; set; }

    /// <summary>Цена товара на момент оформления заказа.</summary>
    public decimal UnitPrice { get; set; }
}
