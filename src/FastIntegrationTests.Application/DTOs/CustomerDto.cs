namespace FastIntegrationTests.Application.DTOs;

/// <summary>Данные покупателя, возвращаемые клиенту.</summary>
public class CustomerDto
{
    /// <summary>Идентификатор покупателя.</summary>
    public Guid Id { get; set; }

    /// <summary>Имя покупателя.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Электронная почта.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Номер телефона.</summary>
    public string? Phone { get; set; }

    /// <summary>Статус покупателя.</summary>
    public CustomerStatus Status { get; set; }

    /// <summary>Дата и время создания.</summary>
    public DateTime CreatedAt { get; set; }
}
