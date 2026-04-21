namespace FastIntegrationTests.Infrastructure.Data.Configurations;

/// <summary>Конфигурация EF Core для сущности <see cref="Discount"/>.</summary>
public class DiscountConfiguration : IEntityTypeConfiguration<Discount>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Discount> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Code).IsRequired().HasMaxLength(100);
        builder.Property(d => d.DiscountPercent).IsRequired();
        builder.Property(d => d.IsActive).IsRequired();
        builder.Property(d => d.CreatedAt).IsRequired();
        builder.HasIndex(d => d.Code).IsUnique();
        builder.ToTable(t => t.HasCheckConstraint("CK_Discounts_DiscountPercent", "\"DiscountPercent\" BETWEEN 1 AND 100"));
    }
}
