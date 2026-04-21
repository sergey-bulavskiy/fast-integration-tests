namespace FastIntegrationTests.Application.DTOs;

/// <summary>Данные поставщика, возвращаемые клиенту.</summary>
public class SupplierDto
{
    /// <summary>Идентификатор поставщика.</summary>
    public Guid Id { get; set; }

    /// <summary>Название поставщика.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Контактный email.</summary>
    public string ContactEmail { get; set; } = string.Empty;

    /// <summary>Страна поставщика.</summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>Активен ли поставщик.</summary>
    public bool IsActive { get; set; }

    /// <summary>Дата и время создания.</summary>
    public DateTime CreatedAt { get; set; }
}
