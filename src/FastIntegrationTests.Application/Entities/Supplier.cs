namespace FastIntegrationTests.Application.Entities;

/// <summary>Поставщик.</summary>
public class Supplier
{
    /// <summary>Уникальный идентификатор.</summary>
    public Guid Id { get; set; }

    /// <summary>Название поставщика.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Контактный email (уникален).</summary>
    public string ContactEmail { get; set; } = string.Empty;

    /// <summary>Страна поставщика.</summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>Активен ли поставщик.</summary>
    public bool IsActive { get; set; }

    /// <summary>Дата и время создания (UTC).</summary>
    public DateTime CreatedAt { get; set; }
}
