namespace FastIntegrationTests.Application.DTOs;

/// <summary>
/// Данные заказа, возвращаемые клиенту.
/// </summary>
public class OrderDto
{
    /// <summary>Идентификатор заказа.</summary>
    public int Id { get; set; }

    /// <summary>Дата и время создания заказа.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Текущий статус заказа.</summary>
    public OrderStatus Status { get; set; }

    /// <summary>Итоговая сумма заказа.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Позиции заказа.</summary>
    public List<OrderItemDto> Items { get; set; } = new();
}
