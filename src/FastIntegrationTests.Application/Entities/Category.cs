namespace FastIntegrationTests.Application.Entities;

/// <summary>Категория товаров.</summary>
public class Category
{
    /// <summary>Уникальный идентификатор.</summary>
    public Guid Id { get; set; }

    /// <summary>Название категории (уникально).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Описание категории.</summary>
    public string? Description { get; set; }

    /// <summary>Дата и время создания (UTC).</summary>
    public DateTime CreatedAt { get; set; }
}
