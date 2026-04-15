namespace FastIntegrationTests.Application.DTOs;

/// <summary>
/// Данные товара, возвращаемые клиенту.
/// </summary>
public class ProductDto
{
    /// <summary>Идентификатор товара.</summary>
    public int Id { get; set; }

    /// <summary>Название товара.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Описание товара.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Текущая цена товара.</summary>
    public decimal Price { get; set; }

    /// <summary>Дата и время создания.</summary>
    public DateTime CreatedAt { get; set; }
}
