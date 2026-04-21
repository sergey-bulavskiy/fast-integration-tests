namespace FastIntegrationTests.Infrastructure.Repositories;

/// <summary>Репозиторий покупателей на основе EF Core.</summary>
public class CustomerRepository : ICustomerRepository
{
    private readonly ShopDbContext _context;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="CustomerRepository"/>.
    /// </summary>
    /// <param name="context">Контекст базы данных.</param>
    public CustomerRepository(ShopDbContext context) => _context = context;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct = default)
        => await _context.Customers.AsNoTracking().ToListAsync(ct);

    /// <inheritdoc/>
    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);

    /// <inheritdoc/>
    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
        => await _context.Customers.AnyAsync(c => c.Email == email, ct);

    /// <inheritdoc/>
    public async Task<Customer> AddAsync(Customer customer, CancellationToken ct = default)
    {
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync(ct);
        return customer;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Customer customer, CancellationToken ct = default)
    {
        _context.Customers.Update(customer);
        await _context.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Customer customer, CancellationToken ct = default)
    {
        await _context.Customers
            .Where(c => c.Id == customer.Id)
            .ExecuteDeleteAsync(ct);
    }
}
