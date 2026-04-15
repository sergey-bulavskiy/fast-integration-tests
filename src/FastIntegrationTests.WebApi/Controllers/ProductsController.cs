namespace FastIntegrationTests.WebApi.Controllers;

/// <summary>
/// Контроллер для управления товарами каталога.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="ProductsController"/>.
    /// </summary>
    /// <param name="productService">Сервис управления товарами.</param>
    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>
    /// Возвращает список всех товаров.
    /// </summary>
    /// <param name="ct">Токен отмены операции.</param>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetAll(CancellationToken ct)
        => Ok(await _productService.GetAllAsync(ct));

    /// <summary>
    /// Возвращает товар по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор товара.</param>
    /// <param name="ct">Токен отмены операции.</param>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> GetById(int id, CancellationToken ct)
        => Ok(await _productService.GetByIdAsync(id, ct));

    /// <summary>
    /// Создаёт новый товар.
    /// </summary>
    /// <param name="request">Данные для создания товара.</param>
    /// <param name="ct">Токен отмены операции.</param>
    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(CreateProductRequest request, CancellationToken ct)
    {
        var created = await _productService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Обновляет существующий товар.
    /// </summary>
    /// <param name="id">Идентификатор товара.</param>
    /// <param name="ct">Токен отмены операции.</param>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductDto>> Update(
        int id,
        UpdateProductRequest request,
        CancellationToken ct)
        => Ok(await _productService.UpdateAsync(id, request, ct));

    /// <summary>
    /// Удаляет товар по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор товара.</param>
    /// <param name="ct">Токен отмены операции.</param>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _productService.DeleteAsync(id, ct);
        return NoContent();
    }
}
