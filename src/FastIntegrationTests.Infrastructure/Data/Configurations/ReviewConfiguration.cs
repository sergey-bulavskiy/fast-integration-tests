namespace FastIntegrationTests.Infrastructure.Data.Configurations;

/// <summary>Конфигурация EF Core для сущности <see cref="Review"/>.</summary>
public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Title).IsRequired().HasMaxLength(200);
        builder.Property(r => r.Body).IsRequired().HasMaxLength(4000);
        builder.Property(r => r.Rating).IsRequired();
        builder.Property(r => r.Status).IsRequired();
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_Reviews_Rating", "\"Rating\" BETWEEN 1 AND 5"));
    }
}
