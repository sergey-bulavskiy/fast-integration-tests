using FastIntegrationTests.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace FastIntegrationTests.Tests.Infrastructure.WebApp;

/// <summary>
/// WebApplicationFactory с подменой строки подключения к PostgreSQL.
/// Переопределяет ConnectionStrings через ConfigureAppConfiguration
/// и заменяет регистрацию DbContext через ConfigureTestServices.
/// </summary>
public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="TestWebApplicationFactory"/>.
    /// </summary>
    /// <param name="connectionString">Строка подключения к тестовой БД PostgreSQL.</param>
    public TestWebApplicationFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Подавляем вывод приложения во время тестов
        builder.ConfigureLogging(logging => logging.ClearProviders());

        // Подменяем строку подключения: Program.cs читает ConnectionStrings:PostgreSQL
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PostgreSQL"] = _connectionString,
            });
        });

        // Гарантируем правильный DbContext на случай, если Program.cs уже зарегистрировал другой
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ShopDbContext>>();
            services.RemoveAll<ShopDbContext>();
            services.AddDbContext<ShopDbContext>(o => o.UseNpgsql(_connectionString));
        });
    }
}
