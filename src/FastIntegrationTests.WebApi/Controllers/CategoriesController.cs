namespace FastIntegrationTests.WebApi.Controllers;

/// <summary>
/// Контроллер для управления категориями товаров.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="CategoriesController"/>.
    /// </summary>
    /// <param name="categoryService">Сервис категорий.</param>
    public CategoriesController(ICategoryService categoryService)
        => _categoryService = categoryService;

    /// <summary>GET /api/categories — возвращает все категории.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetAll(CancellationToken ct)
        => Ok(await _categoryService.GetAllAsync(ct));

    /// <summary>GET /api/categories/{id} — возвращает категорию по идентификатору.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CategoryDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await _categoryService.GetByIdAsync(id, ct));

    /// <summary>POST /api/categories — создаёт новую категорию.</summary>
    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create(CreateCategoryRequest request, CancellationToken ct)
    {
        var created = await _categoryService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>PUT /api/categories/{id} — обновляет категорию.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CategoryDto>> Update(Guid id, UpdateCategoryRequest request, CancellationToken ct)
        => Ok(await _categoryService.UpdateAsync(id, request, ct));

    /// <summary>DELETE /api/categories/{id} — удаляет категорию.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _categoryService.DeleteAsync(id, ct);
        return NoContent();
    }
}
