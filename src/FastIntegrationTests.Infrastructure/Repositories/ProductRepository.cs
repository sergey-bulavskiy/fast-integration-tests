namespace FastIntegrationTests.Infrastructure.Repositories;

/// <summary>
/// Реализация репозитория товаров на основе EF Core.
/// </summary>
public class ProductRepository : IProductRepository
{
    private readonly ShopDbContext _context;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="ProductRepository"/>.
    /// </summary>
    /// <param name="context">Контекст базы данных.</param>
    public ProductRepository(ShopDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default)
        => await _context.Products.AsNoTracking().ToListAsync(ct);

    /// <inheritdoc/>
    public async Task<Product?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _context.Products.FirstOrDefaultAsync(p => p.Id == id, ct);

    /// <inheritdoc/>
    public async Task<Product> AddAsync(Product product, CancellationToken ct = default)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync(ct);
        return product;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Product product, CancellationToken ct = default)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Product product, CancellationToken ct = default)
    {
        try
        {
            await _context.Products
                .Where(p => p.Id == product.Id)
                .ExecuteDeleteAsync(ct);
        }
        catch (System.Data.Common.DbException ex)
        {
            throw new DbUpdateException("Не удалось удалить товар из-за ограничений базы данных.", ex);
        }
    }
}
