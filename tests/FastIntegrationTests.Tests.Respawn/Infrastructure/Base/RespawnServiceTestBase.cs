namespace FastIntegrationTests.Tests.Infrastructure.Base;

/// <summary>
/// Базовый класс для интеграционных тестов сервисного уровня через Respawn.
/// Миграции применяются один раз на класс; данные сбрасываются перед каждым тестом (~1 мс).
/// </summary>
public abstract class RespawnServiceTestBase : IAsyncLifetime, IClassFixture<RespawnFixture>
{
    private readonly RespawnFixture _fixture;

    /// <summary>Контекст тестовой БД. Доступен после InitializeAsync.</summary>
    protected ShopDbContext Context { get; private set; } = null!;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="RespawnServiceTestBase"/>.
    /// </summary>
    /// <param name="fixture">Фикстура с контейнером и Respawner.</param>
    protected RespawnServiceTestBase(RespawnFixture fixture) => _fixture = fixture;

    /// <inheritdoc />
    public virtual async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        var options = new DbContextOptionsBuilder<ShopDbContext>()
            .UseNpgsql(_fixture.ConnectionString).Options;
        Context = new ShopDbContext(options);
    }

    /// <inheritdoc />
    public virtual async Task DisposeAsync()
    {
        await Context.DisposeAsync();
    }
}
