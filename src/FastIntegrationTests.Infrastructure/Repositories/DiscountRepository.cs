namespace FastIntegrationTests.Infrastructure.Repositories;

/// <summary>Репозиторий скидок на основе EF Core.</summary>
public class DiscountRepository : IDiscountRepository
{
    private readonly ShopDbContext _context;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="DiscountRepository"/>.
    /// </summary>
    /// <param name="context">Контекст базы данных.</param>
    public DiscountRepository(ShopDbContext context) => _context = context;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Discount>> GetAllAsync(CancellationToken ct = default)
        => await _context.Discounts.AsNoTracking().ToListAsync(ct);

    /// <inheritdoc/>
    public async Task<Discount?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Discounts.FirstOrDefaultAsync(d => d.Id == id, ct);

    /// <inheritdoc/>
    public async Task<bool> ExistsByCodeAsync(string code, CancellationToken ct = default)
        => await _context.Discounts.AnyAsync(d => d.Code == code, ct);

    /// <inheritdoc/>
    public async Task<Discount> AddAsync(Discount discount, CancellationToken ct = default)
    {
        _context.Discounts.Add(discount);
        await _context.SaveChangesAsync(ct);
        return discount;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Discount discount, CancellationToken ct = default)
    {
        _context.Discounts.Update(discount);
        await _context.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Discount discount, CancellationToken ct = default)
    {
        await _context.Discounts
            .Where(d => d.Id == discount.Id)
            .ExecuteDeleteAsync(ct);
    }
}
