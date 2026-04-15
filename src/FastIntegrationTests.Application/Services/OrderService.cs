namespace FastIntegrationTests.Application.Services;

/// <summary>
/// Сервис для управления заказами и их жизненным циклом.
/// </summary>
public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="OrderService"/>.
    /// </summary>
    /// <param name="orderRepository">Репозиторий заказов.</param>
    /// <param name="productRepository">Репозиторий товаров.</param>
    public OrderService(IOrderRepository orderRepository, IProductRepository productRepository)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
    }

    /// <summary>
    /// Возвращает список всех заказов.
    /// </summary>
    public async Task<IReadOnlyList<OrderDto>> GetAllAsync(CancellationToken ct = default)
    {
        var orders = await _orderRepository.GetAllAsync(ct);
        return orders.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Возвращает заказ вместе с позициями по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор заказа.</param>
    /// <exception cref="NotFoundException">Если заказ не найден.</exception>
    public async Task<OrderDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var order = await _orderRepository.GetByIdWithItemsAsync(id, ct)
            ?? throw new NotFoundException(nameof(Order), id);
        return MapToDto(order);
    }

    /// <summary>
    /// Создаёт новый заказ. Фиксирует цены товаров и рассчитывает итоговую сумму.
    /// </summary>
    /// <param name="request">Данные нового заказа с позициями.</param>
    /// <exception cref="NotFoundException">Если один из указанных товаров не найден.</exception>
    public async Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken ct = default)
    {
        var order = new Order
        {
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.New,
            Items = new List<OrderItem>(),
        };

        foreach (var itemRequest in request.Items)
        {
            var product = await _productRepository.GetByIdAsync(itemRequest.ProductId, ct)
                ?? throw new NotFoundException(nameof(Product), itemRequest.ProductId);

            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                Quantity = itemRequest.Quantity,
                UnitPrice = product.Price,
            });
        }

        order.TotalAmount = order.Items.Sum(i => i.UnitPrice * i.Quantity);

        var created = await _orderRepository.AddAsync(order, ct);
        return MapToDto(created);
    }

    /// <summary>Подтверждает заказ (New → Confirmed).</summary>
    /// <exception cref="NotFoundException">Если заказ не найден.</exception>
    /// <exception cref="InvalidOrderStatusTransitionException">Если переход недопустим.</exception>
    public Task<OrderDto> ConfirmAsync(int id, CancellationToken ct = default)
        => ChangeStatusAsync(id, OrderStatus.Confirmed, ct);

    /// <summary>Передаёт заказ в доставку (Confirmed → Shipped).</summary>
    /// <exception cref="NotFoundException">Если заказ не найден.</exception>
    /// <exception cref="InvalidOrderStatusTransitionException">Если переход недопустим.</exception>
    public Task<OrderDto> ShipAsync(int id, CancellationToken ct = default)
        => ChangeStatusAsync(id, OrderStatus.Shipped, ct);

    /// <summary>Завершает заказ (Shipped → Completed).</summary>
    /// <exception cref="NotFoundException">Если заказ не найден.</exception>
    /// <exception cref="InvalidOrderStatusTransitionException">Если переход недопустим.</exception>
    public Task<OrderDto> CompleteAsync(int id, CancellationToken ct = default)
        => ChangeStatusAsync(id, OrderStatus.Completed, ct);

    /// <summary>Отменяет заказ (New/Confirmed → Cancelled).</summary>
    /// <exception cref="NotFoundException">Если заказ не найден.</exception>
    /// <exception cref="InvalidOrderStatusTransitionException">Если переход недопустим.</exception>
    public Task<OrderDto> CancelAsync(int id, CancellationToken ct = default)
        => ChangeStatusAsync(id, OrderStatus.Cancelled, ct);

    private async Task<OrderDto> ChangeStatusAsync(int id, OrderStatus targetStatus, CancellationToken ct)
    {
        var order = await _orderRepository.GetByIdWithItemsAsync(id, ct)
            ?? throw new NotFoundException(nameof(Order), id);

        ValidateStatusTransition(order.Status, targetStatus);
        order.Status = targetStatus;
        await _orderRepository.UpdateAsync(order, ct);
        return MapToDto(order);
    }

    /// <summary>
    /// Проверяет допустимость перехода статуса заказа.
    /// Допустимые переходы: New→Confirmed, New→Cancelled, Confirmed→Shipped,
    /// Confirmed→Cancelled, Shipped→Completed.
    /// </summary>
    private static void ValidateStatusTransition(OrderStatus current, OrderStatus target)
    {
        var allowed = current switch
        {
            OrderStatus.New => new[] { OrderStatus.Confirmed, OrderStatus.Cancelled },
            OrderStatus.Confirmed => new[] { OrderStatus.Shipped, OrderStatus.Cancelled },
            OrderStatus.Shipped => new[] { OrderStatus.Completed },
            _ => Array.Empty<OrderStatus>(),
        };

        if (!allowed.Contains(target))
            throw new InvalidOrderStatusTransitionException(current, target);
    }

    private static OrderDto MapToDto(Order o) => new()
    {
        Id = o.Id,
        CreatedAt = o.CreatedAt,
        Status = o.Status,
        TotalAmount = o.TotalAmount,
        Items = o.Items.Select(i => new OrderItemDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
        }).ToList(),
    };
}
