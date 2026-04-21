namespace FastIntegrationTests.WebApi.Controllers;

/// <summary>
/// Контроллер для управления поставщиками.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierService _supplierService;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="SuppliersController"/>.
    /// </summary>
    /// <param name="supplierService">Сервис поставщиков.</param>
    public SuppliersController(ISupplierService supplierService)
        => _supplierService = supplierService;

    /// <summary>GET /api/suppliers — возвращает всех поставщиков.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SupplierDto>>> GetAll(CancellationToken ct)
        => Ok(await _supplierService.GetAllAsync(ct));

    /// <summary>GET /api/suppliers/{id} — возвращает поставщика по идентификатору.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SupplierDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await _supplierService.GetByIdAsync(id, ct));

    /// <summary>POST /api/suppliers — создаёт нового поставщика.</summary>
    [HttpPost]
    public async Task<ActionResult<SupplierDto>> Create(CreateSupplierRequest request, CancellationToken ct)
    {
        var created = await _supplierService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>PUT /api/suppliers/{id} — обновляет данные поставщика.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SupplierDto>> Update(Guid id, UpdateSupplierRequest request, CancellationToken ct)
        => Ok(await _supplierService.UpdateAsync(id, request, ct));

    /// <summary>DELETE /api/suppliers/{id} — удаляет поставщика.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _supplierService.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>POST /api/suppliers/{id}/activate — активирует поставщика.</summary>
    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        await _supplierService.ActivateAsync(id, ct);
        return NoContent();
    }

    /// <summary>POST /api/suppliers/{id}/deactivate — деактивирует поставщика.</summary>
    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await _supplierService.DeactivateAsync(id, ct);
        return NoContent();
    }
}
