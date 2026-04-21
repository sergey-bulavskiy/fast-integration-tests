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

    /// <summary>Категории товаров.</summary>
    public DbSet<Category> Categories => Set<Category>();

    /// <summary>Покупатели.</summary>
    public DbSet<Customer> Customers => Set<Customer>();

    /// <summary>Поставщики.</summary>
    public DbSet<Supplier> Suppliers => Set<Supplier>();

    /// <summary>Отзывы.</summary>
    public DbSet<Review> Reviews => Set<Review>();

    /// <summary>Скидки.</summary>
    public DbSet<Discount> Discounts => Set<Discount>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Применяем все конфигурации IEntityTypeConfiguration из текущей сборки
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ShopDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
