namespace FastIntegrationTests.Infrastructure.Repositories;

/// <summary>
/// Реализация репозитория заказов на основе EF Core.
/// </summary>
public class OrderRepository : IOrderRepository
{
    private readonly ShopDbContext _context;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="OrderRepository"/>.
    /// </summary>
    /// <param name="context">Контекст базы данных.</param>
    public OrderRepository(ShopDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken ct = default)
        => await _context.Orders.AsNoTracking().ToListAsync(ct);

    /// <inheritdoc/>
    public async Task<Order?> GetByIdWithItemsAsync(int id, CancellationToken ct = default)
        => await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    /// <inheritdoc/>
    public async Task<Order> AddAsync(Order order, CancellationToken ct = default)
    {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync(ct);
        return order;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Order order, CancellationToken ct = default)
    {
        _context.Orders.Update(order);
        await _context.SaveChangesAsync(ct);
    }
}
