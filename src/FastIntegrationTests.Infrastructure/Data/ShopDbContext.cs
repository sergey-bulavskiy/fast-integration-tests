namespace FastIntegrationTests.Infrastructure.Data;

/// <summary>
/// Контекст базы данных магазина.
/// Содержит DbSet для всех доменных сущностей и применяет конфигурации EF Core из текущей сборки.
/// </summary>
public class ShopDbContext : DbContext
{
    /// <summary>
    /// Создаёт новый экземпляр <see cref="ShopDbContext"/>.
    /// </summary>
    /// <param name="options">Параметры контекста, включая провайдер и строку подключения.</param>
    public ShopDbContext(DbContextOptions<ShopDbContext> options) : base(options)
    {
    }

    /// <summary>Товары каталога.</summary>
    public DbSet<Product> Products => Set<Product>();

    /// <summary>Заказы.</summary>
    public DbSet<Order> Orders => Set<Order>();

    /// <summary>Позиции заказов.</summary>
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Применяем все конфигурации IEntityTypeConfiguration из текущей сборки
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ShopDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
