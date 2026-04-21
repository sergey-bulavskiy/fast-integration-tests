namespace FastIntegrationTests.Application.DTOs;

/// <summary>Запрос на создание отзыва.</summary>
public class CreateReviewRequest
{
    /// <summary>Заголовок отзыва.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Текст отзыва.</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>Рейтинг (1–5).</summary>
    public int Rating { get; set; }
}
