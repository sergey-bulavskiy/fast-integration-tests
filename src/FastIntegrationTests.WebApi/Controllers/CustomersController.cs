namespace FastIntegrationTests.WebApi.Controllers;

/// <summary>
/// Контроллер для управления покупателями.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="CustomersController"/>.
    /// </summary>
    /// <param name="customerService">Сервис покупателей.</param>
    public CustomersController(ICustomerService customerService)
        => _customerService = customerService;

    /// <summary>GET /api/customers — возвращает всех покупателей.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerDto>>> GetAll(CancellationToken ct)
        => Ok(await _customerService.GetAllAsync(ct));

    /// <summary>GET /api/customers/{id} — возвращает покупателя по идентификатору.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await _customerService.GetByIdAsync(id, ct));

    /// <summary>POST /api/customers — создаёт нового покупателя.</summary>
    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create(CreateCustomerRequest request, CancellationToken ct)
    {
        var created = await _customerService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>PUT /api/customers/{id} — обновляет данные покупателя.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CustomerDto>> Update(Guid id, UpdateCustomerRequest request, CancellationToken ct)
        => Ok(await _customerService.UpdateAsync(id, request, ct));

    /// <summary>DELETE /api/customers/{id} — удаляет покупателя.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _customerService.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>POST /api/customers/{id}/ban — блокирует покупателя.</summary>
    [HttpPost("{id:guid}/ban")]
    public async Task<IActionResult> Ban(Guid id, CancellationToken ct)
    {
        await _customerService.BanAsync(id, ct);
        return NoContent();
    }

    /// <summary>POST /api/customers/{id}/activate — активирует покупателя.</summary>
    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        await _customerService.ActivateAsync(id, ct);
        return NoContent();
    }

    /// <summary>POST /api/customers/{id}/deactivate — деактивирует покупателя.</summary>
    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await _customerService.DeactivateAsync(id, ct);
        return NoContent();
    }
}
