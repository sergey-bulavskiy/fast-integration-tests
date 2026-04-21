namespace FastIntegrationTests.Application.DTOs;

/// <summary>Данные скидки, возвращаемые клиенту.</summary>
public class DiscountDto
{
    /// <summary>Идентификатор скидки.</summary>
    public Guid Id { get; set; }

    /// <summary>Код скидки.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Процент скидки (1–100).</summary>
    public int DiscountPercent { get; set; }

    /// <summary>Активна ли скидка.</summary>
    public bool IsActive { get; set; }

    /// <summary>Дата истечения скидки (UTC).</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>Дата и время создания.</summary>
    public DateTime CreatedAt { get; set; }
}
