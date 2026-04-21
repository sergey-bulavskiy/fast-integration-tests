namespace FastIntegrationTests.Application.DTOs;

/// <summary>Запрос на обновление скидки.</summary>
public class UpdateDiscountRequest
{
    /// <summary>Новый код скидки.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Новый процент скидки (1–100).</summary>
    public int DiscountPercent { get; set; }

    /// <summary>Новая дата истечения скидки (UTC, опционально).</summary>
    public DateTime? ExpiresAt { get; set; }
}
