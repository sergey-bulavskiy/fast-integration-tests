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

        var csb = new NpgsqlConnectionStringBuilder(_fixture.ConnectionString)
        {
            Database = dbName
        };
        var options = new DbContextOptionsBuilder<ShopDbContext>()
            .UseNpgsql(csb.ConnectionString)
            .Options;

        var context = new ShopDbContext(options);
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            await context.Database.MigrateAsync(ct);
            sw.Stop();
            Console.WriteLine($"##BENCH[migration]={sw.ElapsedMilliseconds}");
        }
        catch
        {
            await context.DisposeAsync();
            throw;
        }
        return context;
    }
}
