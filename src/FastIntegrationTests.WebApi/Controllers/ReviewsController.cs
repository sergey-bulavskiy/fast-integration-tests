namespace FastIntegrationTests.WebApi.Controllers;

/// <summary>
/// Контроллер для управления отзывами.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="ReviewsController"/>.
    /// </summary>
    /// <param name="reviewService">Сервис отзывов.</param>
    public ReviewsController(IReviewService reviewService)
        => _reviewService = reviewService;

    /// <summary>GET /api/reviews — возвращает все отзывы.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ReviewDto>>> GetAll(CancellationToken ct)
        => Ok(await _reviewService.GetAllAsync(ct));

    /// <summary>GET /api/reviews/{id} — возвращает отзыв по идентификатору.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReviewDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await _reviewService.GetByIdAsync(id, ct));

    /// <summary>POST /api/reviews — создаёт новый отзыв.</summary>
    [HttpPost]
    public async Task<ActionResult<ReviewDto>> Create(CreateReviewRequest request, CancellationToken ct)
    {
        var created = await _reviewService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>DELETE /api/reviews/{id} — удаляет отзыв.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _reviewService.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>POST /api/reviews/{id}/approve — одобряет отзыв.</summary>
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        await _reviewService.ApproveAsync(id, ct);
        return NoContent();
    }

    /// <summary>POST /api/reviews/{id}/reject — отклоняет отзыв.</summary>
    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, CancellationToken ct)
    {
        await _reviewService.RejectAsync(id, ct);
        return NoContent();
    }
}
