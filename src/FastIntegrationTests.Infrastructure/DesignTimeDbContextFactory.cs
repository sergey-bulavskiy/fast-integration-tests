using Microsoft.EntityFrameworkCore.Design;

namespace FastIntegrationTests.Infrastructure;

/// <summary>
/// Фабрика DbContext для инструментов EF Core в design-time (миграции, scaffolding).
/// Используется только командами dotnet ef — не влияет на production-конфигурацию.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ShopDbContext>
{
    /// <inheritdoc/>
    public ShopDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ShopDbContext>()
            .UseNpgsql("Host=localhost;Database=shop;Username=postgres;Password=postgres")
            .Options;
        return new ShopDbContext(options);
    }
}
