namespace FastIntegrationTests.Application.DTOs;

/// <summary>Данные категории, возвращаемые клиенту.</summary>
public class CategoryDto
{
    /// <summary>Идентификатор категории.</summary>
    public Guid Id { get; set; }

    /// <summary>Название категории.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Описание категории.</summary>
    public string? Description { get; set; }

    /// <summary>Дата и время создания.</summary>
    public DateTime CreatedAt { get; set; }
}
