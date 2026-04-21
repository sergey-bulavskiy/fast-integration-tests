namespace FastIntegrationTests.Application.DTOs;

/// <summary>Запрос на обновление данных покупателя.</summary>
public class UpdateCustomerRequest
{
    /// <summary>Новое имя покупателя.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Новая электронная почта.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Новый номер телефона.</summary>
    public string? Phone { get; set; }
}
