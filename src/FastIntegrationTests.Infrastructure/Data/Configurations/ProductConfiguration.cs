namespace FastIntegrationTests.Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация таблицы товаров для EF Core.
/// </summary>
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Description).HasMaxLength(1000);
        // decimal(18,2) совместим с PostgreSQL (numeric) и MSSQL (decimal)
        builder.Property(p => p.Price).HasColumnType("decimal(18,2)");
    }
}
