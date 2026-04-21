namespace FastIntegrationTests.Application.Entities;

/// <summary>Скидка.</summary>
public class Discount
{
    /// <summary>Уникальный идентификатор.</summary>
    public Guid Id { get; set; }

    /// <summary>Код скидки (уникален).</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Процент скидки (1–100).</summary>
    public int DiscountPercent { get; set; }

    /// <summary>Активна ли скидка.</summary>
    public bool IsActive { get; set; }

    /// <summary>Дата истечения скидки (UTC, опционально).</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>Дата и время создания (UTC).</summary>
    public DateTime CreatedAt { get; set; }
}
