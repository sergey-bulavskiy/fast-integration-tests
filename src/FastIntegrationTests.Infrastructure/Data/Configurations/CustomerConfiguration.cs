namespace FastIntegrationTests.Infrastructure.Data.Configurations;

/// <summary>Конфигурация EF Core для сущности <see cref="Customer"/>.</summary>
public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Email).IsRequired().HasMaxLength(320);
        builder.Property(c => c.Phone).HasMaxLength(50);
        builder.Property(c => c.Status).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.HasIndex(c => c.Email).IsUnique();
    }
}
