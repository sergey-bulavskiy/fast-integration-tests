namespace FastIntegrationTests.Application.DTOs;

/// <summary>
/// Запрос на обновление существующего товара.
/// </summary>
public class UpdateProductRequest
{
    /// <summary>Новое название товара.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Новое описание товара.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Новая цена товара.</summary>
    public decimal Price { get; set; }
}
