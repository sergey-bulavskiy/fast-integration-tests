namespace FastIntegrationTests.Application.Entities;

/// <summary>Отзыв.</summary>
public class Review
{
    /// <summary>Уникальный идентификатор.</summary>
    public Guid Id { get; set; }

    /// <summary>Заголовок отзыва.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Текст отзыва.</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>Рейтинг (1–5).</summary>
    public int Rating { get; set; }

    /// <summary>Статус отзыва.</summary>
    public ReviewStatus Status { get; set; }

    /// <summary>Дата и время создания (UTC).</summary>
    public DateTime CreatedAt { get; set; }
}
