namespace FastIntegrationTests.Infrastructure.Repositories;

/// <summary>Репозиторий категорий на основе EF Core.</summary>
public class CategoryRepository : ICategoryRepository
{
    private readonly ShopDbContext _context;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="CategoryRepository"/>.
    /// </summary>
    /// <param name="context">Контекст базы данных.</param>
    public CategoryRepository(ShopDbContext context) => _context = context;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default)
        => await _context.Categories.AsNoTracking().ToListAsync(ct);

    /// <inheritdoc/>
    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);

    /// <inheritdoc/>
    public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default)
        => await _context.Categories.AnyAsync(c => c.Name == name, ct);

    /// <inheritdoc/>
    public async Task<Category> AddAsync(Category category, CancellationToken ct = default)
    {
        _context.Categories.Add(category);
        await _context.SaveChangesAsync(ct);
        return category;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Category category, CancellationToken ct = default)
    {
        _context.Categories.Update(category);
        await _context.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Category category, CancellationToken ct = default)
    {
        await _context.Categories
            .Where(c => c.Id == category.Id)
            .ExecuteDeleteAsync(ct);
    }
}
