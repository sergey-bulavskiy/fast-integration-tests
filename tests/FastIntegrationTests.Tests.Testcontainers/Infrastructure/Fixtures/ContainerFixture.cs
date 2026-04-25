using Testcontainers.PostgreSql;

namespace FastIntegrationTests.Tests.Infrastructure.Fixtures;

/// <summary>
/// Запускает один Testcontainers-контейнер PostgreSQL на всю коллекцию тестов.
/// </summary>
public sealed class ContainerFixture : IAsyncLifetime
{
    private PostgreSqlContainer _container = null!;

    /// <summary>Базовая строка подключения к контейнеру (без конкретной БД).</summary>
    public string ConnectionString { get; private set; } = null!;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .Build();
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }
}
