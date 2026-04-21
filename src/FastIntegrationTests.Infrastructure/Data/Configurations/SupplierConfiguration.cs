namespace FastIntegrationTests.Infrastructure.Data.Configurations;

/// <summary>Конфигурация EF Core для сущности <see cref="Supplier"/>.</summary>
public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.ContactEmail).IsRequired().HasMaxLength(320);
        builder.Property(s => s.Country).IsRequired().HasMaxLength(100);
        builder.Property(s => s.IsActive).IsRequired();
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.HasIndex(s => s.ContactEmail).IsUnique();
    }
}
