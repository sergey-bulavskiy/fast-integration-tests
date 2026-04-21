namespace FastIntegrationTests.Infrastructure.Repositories;

/// <summary>Репозиторий отзывов на основе EF Core.</summary>
public class ReviewRepository : IReviewRepository
{
    private readonly ShopDbContext _context;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="ReviewRepository"/>.
    /// </summary>
    /// <param name="context">Контекст базы данных.</param>
    public ReviewRepository(ShopDbContext context) => _context = context;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Review>> GetAllAsync(CancellationToken ct = default)
        => await _context.Reviews.AsNoTracking().ToListAsync(ct);

    /// <inheritdoc/>
    public async Task<Review?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Reviews.FirstOrDefaultAsync(r => r.Id == id, ct);

    /// <inheritdoc/>
    public async Task<Review> AddAsync(Review review, CancellationToken ct = default)
    {
        _context.Reviews.Add(review);
        await _context.SaveChangesAsync(ct);
        return review;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Review review, CancellationToken ct = default)
    {
        _context.Reviews.Update(review);
        await _context.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Review review, CancellationToken ct = default)
    {
        await _context.Reviews
            .Where(r => r.Id == review.Id)
            .ExecuteDeleteAsync(ct);
    }
}
