namespace FastIntegrationTests.Application.DTOs;

/// <summary>Запрос на создание скидки.</summary>
public class CreateDiscountRequest
{
    /// <summary>Код скидки.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Процент скидки (1–100).</summary>
    public int DiscountPercent { get; set; }

    /// <summary>Дата истечения скидки (UTC, опционально).</summary>
    public DateTime? ExpiresAt { get; set; }
}
