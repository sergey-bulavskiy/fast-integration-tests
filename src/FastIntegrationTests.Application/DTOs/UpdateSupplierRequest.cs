namespace FastIntegrationTests.Application.DTOs;

/// <summary>Запрос на обновление поставщика.</summary>
public class UpdateSupplierRequest
{
    /// <summary>Новое название поставщика.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Новый контактный email.</summary>
    public string ContactEmail { get; set; } = string.Empty;

    /// <summary>Новая страна поставщика.</summary>
    public string Country { get; set; } = string.Empty;
}
