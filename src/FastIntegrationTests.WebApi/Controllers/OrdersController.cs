namespace FastIntegrationTests.WebApi.Controllers;

/// <summary>
/// Контроллер для управления заказами и их жизненным циклом.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="OrdersController"/>.
    /// </summary>
    /// <param name="orderService">Сервис управления заказами.</param>
    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// Возвращает список всех заказов.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetAll(CancellationToken ct)
        => Ok(await _orderService.GetAllAsync(ct));

    /// <summary>
    /// Возвращает заказ с позициями по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор заказа.</param>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDto>> GetById(int id, CancellationToken ct)
        => Ok(await _orderService.GetByIdAsync(id, ct));

    /// <summary>
    /// Создаёт новый заказ.
    /// </summary>
    /// <param name="request">Данные для создания заказа.</param>
    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderRequest request, CancellationToken ct)
    {
        var created = await _orderService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Подтверждает заказ (New → Confirmed).
    /// </summary>
    /// <param name="id">Идентификатор заказа.</param>
    [HttpPost("{id:int}/confirm")]
    public async Task<ActionResult<OrderDto>> Confirm(int id, CancellationToken ct)
        => Ok(await _orderService.ConfirmAsync(id, ct));

    /// <summary>
    /// Передаёт заказ в доставку (Confirmed → Shipped).
    /// </summary>
    /// <param name="id">Идентификатор заказа.</param>
    [HttpPost("{id:int}/ship")]
    public async Task<ActionResult<OrderDto>> Ship(int id, CancellationToken ct)
        => Ok(await _orderService.ShipAsync(id, ct));

    /// <summary>
    /// Завершает заказ (Shipped → Completed).
    /// </summary>
    /// <param name="id">Идентификатор заказа.</param>
    [HttpPost("{id:int}/complete")]
    public async Task<ActionResult<OrderDto>> Complete(int id, CancellationToken ct)
        => Ok(await _orderService.CompleteAsync(id, ct));

    /// <summary>
    /// Отменяет заказ (New/Confirmed → Cancelled).
    /// </summary>
    /// <param name="id">Идентификатор заказа.</param>
    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<OrderDto>> Cancel(int id, CancellationToken ct)
        => Ok(await _orderService.CancelAsync(id, ct));
}
