namespace FastIntegrationTests.Application.DTOs;

/// <summary>
/// Запрос на создание нового товара.
/// </summary>
public class CreateProductRequest
{
    /// <summary>Название товара.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Описание товара.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Цена товара.</summary>
    public decimal Price { get; set; }
}
