namespace FastIntegrationTests.Infrastructure.Repositories;

/// <summary>Репозиторий поставщиков на основе EF Core.</summary>
public class SupplierRepository : ISupplierRepository
{
    private readonly ShopDbContext _context;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="SupplierRepository"/>.
    /// </summary>
    /// <param name="context">Контекст базы данных.</param>
    public SupplierRepository(ShopDbContext context) => _context = context;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Supplier>> GetAllAsync(CancellationToken ct = default)
        => await _context.Suppliers.AsNoTracking().ToListAsync(ct);

    /// <inheritdoc/>
    public async Task<Supplier?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == id, ct);

    /// <inheritdoc/>
    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
        => await _context.Suppliers.AnyAsync(s => s.ContactEmail == email, ct);

    /// <inheritdoc/>
    public async Task<Supplier> AddAsync(Supplier supplier, CancellationToken ct = default)
    {
        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync(ct);
        return supplier;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Supplier supplier, CancellationToken ct = default)
    {
        _context.Suppliers.Update(supplier);
        await _context.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Supplier supplier, CancellationToken ct = default)
    {
        await _context.Suppliers
            .Where(s => s.Id == supplier.Id)
            .ExecuteDeleteAsync(ct);
    }
}
