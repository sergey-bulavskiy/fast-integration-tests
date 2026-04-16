using Microsoft.Extensions.Configuration;
using Testcontainers.MsSql;
using Testcontainers.PostgreSql;

namespace FastIntegrationTests.Tests.Infrastructure.Fixtures;

/// <summary>
/// Запускает один Testcontainers-контейнер (PostgreSQL или MSSQL) на всю коллекцию тестов.
/// Тип контейнера определяется из appsettings.json тест-проекта (DatabaseProvider).
/// </summary>
public sealed class ContainerFixture : IAsyncLifetime
{
    private IAsyncDisposable _container = null!;

    /// <summary>Базовая строка подключения к контейнеру (без конкретной БД).</summary>
    public string ConnectionString { get; private set; } = null!;

    /// <summary>Имя провайдера: "PostgreSQL" или "MSSQL".</summary>
    public string Provider { get; private set; } = null!;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        Provider = config["DatabaseProvider"]
            ?? throw new InvalidOperationException(
                "DatabaseProvider не задан в appsettings.json тест-проекта.");

        if (Provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
        {
            var pg = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .Build();
            await pg.StartAsync();
            ConnectionString = pg.GetConnectionString();
            _container = pg;
        }
        else if (Provider.Equals("MSSQL", StringComparison.OrdinalIgnoreCase))
        {
            var mssql = new MsSqlBuilder()
                .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                .Build();
            await mssql.StartAsync();
            ConnectionString = mssql.GetConnectionString();
            _container = mssql;
        }
        else
        {
            throw new InvalidOperationException(
                $"Неизвестный провайдер '{Provider}'. Допустимые значения: PostgreSQL, MSSQL.");
        }
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }
}
