namespace FastIntegrationTests.Application.DTOs;

/// <summary>Запрос на создание поставщика.</summary>
public class CreateSupplierRequest
{
    /// <summary>Название поставщика.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Контактный email.</summary>
    public string ContactEmail { get; set; } = string.Empty;

    /// <summary>Страна поставщика.</summary>
    public string Country { get; set; } = string.Empty;
}
