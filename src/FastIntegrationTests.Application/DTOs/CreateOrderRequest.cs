namespace FastIntegrationTests.Application.DTOs;

/// <summary>
/// Запрос на создание нового заказа.
/// </summary>
public class CreateOrderRequest
{
    /// <summary>Список позиций заказа.</summary>
    public List<OrderItemRequest> Items { get; set; } = new();
}

/// <summary>
/// Позиция в запросе на создание заказа.
/// </summary>
public class OrderItemRequest
{
    /// <summary>Идентификатор товара.</summary>
    public int ProductId { get; set; }

    /// <summary>Количество единиц товара.</summary>
    public int Quantity { get; set; }
}
