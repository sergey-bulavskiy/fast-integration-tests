namespace FastIntegrationTests.Application.DTOs;

/// <summary>Запрос на создание покупателя.</summary>
public class CreateCustomerRequest
{
    /// <summary>Имя покупателя.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Электронная почта.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Номер телефона.</summary>
    public string? Phone { get; set; }
}
