namespace FastIntegrationTests.Application.Entities;

/// <summary>Покупатель.</summary>
public class Customer
{
    /// <summary>Уникальный идентификатор.</summary>
    public Guid Id { get; set; }

    /// <summary>Имя покупателя.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Электронная почта (уникальна).</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Номер телефона.</summary>
    public string? Phone { get; set; }

    /// <summary>Статус покупателя.</summary>
    public CustomerStatus Status { get; set; }

    /// <summary>Дата и время создания (UTC).</summary>
    public DateTime CreatedAt { get; set; }
}
