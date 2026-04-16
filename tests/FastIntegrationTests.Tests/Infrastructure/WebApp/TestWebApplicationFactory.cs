using FastIntegrationTests.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FastIntegrationTests.Tests.Infrastructure.WebApp;

/// <summary>
/// WebApplicationFactory с подменой строки подключения к БД.
/// Переопределяет DatabaseProvider и ConnectionStrings через ConfigureAppConfiguration,
/// а также заменяет регистрацию DbContext через ConfigureTestServices.
/// </summary>
public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _provider;
    private readonly string _connectionString;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="TestWebApplicationFactory"/>.
    /// </summary>
    /// <param name="provider">Провайдер БД: "PostgreSQL" или "MSSQL".</param>
    /// <param name="connectionString">Строка подключения к тестовой БД.</param>
    public TestWebApplicationFactory(string provider, string connectionString)
    {
        _provider = provider;
        _connectionString = connectionString;
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Подменяем конфигурацию: Program.cs читает DatabaseProvider и ConnectionStrings из builder.Configuration
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabaseProvider"] = _provider,
                [$"ConnectionStrings:{_provider}"] = _connectionString,
            });
        });

        // Гарантируем правильный DbContext на случай, если Program.cs уже зарегистрировал другой
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ShopDbContext>>();
            services.RemoveAll<ShopDbContext>();

            if (_provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
                services.AddDbContext<ShopDbContext>(o => o.UseNpgsql(_connectionString));
            else
                services.AddDbContext<ShopDbContext>(o => o.UseSqlServer(_connectionString));
        });
    }
}
