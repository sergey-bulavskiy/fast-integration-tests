namespace FastIntegrationTests.Application.DTOs;

/// <summary>Запрос на обновление категории.</summary>
public class UpdateCategoryRequest
{
    /// <summary>Новое название категории.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Новое описание категории.</summary>
    public string? Description { get; set; }
}
