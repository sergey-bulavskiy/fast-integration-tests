namespace FastIntegrationTests.Tests.Infrastructure.Base;

/// <summary>
/// Базовый класс для интеграционных тестов сервисного уровня через TestcontainersShared.
/// Контейнер общий на процесс (<see cref="SharedContainerManager"/>);
/// БД создаётся, мигрируется и дропается на каждый тест.
/// </summary>
public abstract class SharedServiceTestBase : IAsyncLifetime
{
    private readonly SharedDbHandle _db = new();

    /// <summary>Контекст тестовой БД. Доступен после <see cref="InitializeAsync"/>.</summary>
    protected ShopDbContext Context { get; private set; } = null!;

    /// <inheritdoc />
    public virtual async Task InitializeAsync()
    {
        await _db.CreateAndMigrateAsync();
        Context = new ShopDbContext(new DbContextOptionsBuilder<ShopDbContext>()
            .UseNpgsql(_db.ConnectionString).Options);
    }

    /// <inheritdoc />
    public virtual async Task DisposeAsync()
    {
        await Context.DisposeAsync();
        await _db.DropAsync();
    }
}
