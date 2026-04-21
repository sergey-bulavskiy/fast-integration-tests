namespace FastIntegrationTests.WebApi.Controllers;

/// <summary>
/// Контроллер для управления скидками.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DiscountsController : ControllerBase
{
    private readonly IDiscountService _discountService;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="DiscountsController"/>.
    /// </summary>
    /// <param name="discountService">Сервис скидок.</param>
    public DiscountsController(IDiscountService discountService)
        => _discountService = discountService;

    /// <summary>GET /api/discounts — возвращает все скидки.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DiscountDto>>> GetAll(CancellationToken ct)
        => Ok(await _discountService.GetAllAsync(ct));

    /// <summary>GET /api/discounts/{id} — возвращает скидку по идентификатору.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DiscountDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await _discountService.GetByIdAsync(id, ct));

    /// <summary>POST /api/discounts — создаёт новую скидку.</summary>
    [HttpPost]
    public async Task<ActionResult<DiscountDto>> Create(CreateDiscountRequest request, CancellationToken ct)
    {
        var created = await _discountService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>PUT /api/discounts/{id} — обновляет скидку.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DiscountDto>> Update(Guid id, UpdateDiscountRequest request, CancellationToken ct)
        => Ok(await _discountService.UpdateAsync(id, request, ct));

    /// <summary>DELETE /api/discounts/{id} — удаляет скидку.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _discountService.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>POST /api/discounts/{id}/activate — активирует скидку.</summary>
    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        await _discountService.ActivateAsync(id, ct);
        return NoContent();
    }

    /// <summary>POST /api/discounts/{id}/deactivate — деактивирует скидку.</summary>
    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await _discountService.DeactivateAsync(id, ct);
        return NoContent();
    }
}
