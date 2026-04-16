using FastIntegrationTests.Infrastructure.Data;
using FastIntegrationTests.Tests.Infrastructure.Fixtures;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FastIntegrationTests.Tests.Infrastructure.Factories;

/// <summary>
/// Создаёт уникальную тестовую базу данных внутри контейнера и применяет миграции EF Core.
/// </summary>
public sealed class TestDbFactory
{
    private readonly ContainerFixture _fixture;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="TestDbFactory"/>.
    /// </summary>
    /// <param name="fixture">Запущенный контейнер с СУБД.</param>
    public TestDbFactory(ContainerFixture fixture) => _fixture = fixture;

    /// <summary>
    /// Создаёт БД с именем <c>test_{guid}</c>, применяет миграции и возвращает контекст.
    /// </summary>
    /// <param name="ct">Токен отмены операции.</param>
    public async Task<ShopDbContext> CreateAsync(CancellationToken ct = default)
    {
        var dbName = $"test_{Guid.NewGuid():N}";

        DbContextOptions<ShopDbContext> options;

        if (_fixture.Provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
        {
            var csb = new NpgsqlConnectionStringBuilder(_fixture.ConnectionString)
            {
                Database = dbName
            };
            options = new DbContextOptionsBuilder<ShopDbContext>()
                .UseNpgsql(csb.ConnectionString)
                .Options;
        }
        else
        {
            var csb = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(_fixture.ConnectionString)
            {
                InitialCatalog = dbName
            };
            options = new DbContextOptionsBuilder<ShopDbContext>()
                .UseSqlServer(csb.ConnectionString)
                .Options;
        }

        var context = new ShopDbContext(options);
        try
        {
            await context.Database.MigrateAsync(ct);
        }
        catch
        {
            await context.DisposeAsync();
            throw;
        }
        return context;
    }
}
