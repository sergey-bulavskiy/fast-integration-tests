namespace FastIntegrationTests.Application.DTOs;

/// <summary>Запрос на создание категории.</summary>
public class CreateCategoryRequest
{
    /// <summary>Название категории.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Описание категории.</summary>
    public string? Description { get; set; }
}
