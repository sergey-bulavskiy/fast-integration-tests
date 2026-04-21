namespace FastIntegrationTests.Infrastructure.Data.Configurations;

/// <summary>Конфигурация EF Core для сущности <see cref="Category"/>.</summary>
public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Description).HasMaxLength(1000);
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.HasIndex(c => c.Name).IsUnique();
    }
}
