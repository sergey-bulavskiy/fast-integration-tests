using FastIntegrationTests.Infrastructure.Repositories;

namespace FastIntegrationTests.Infrastructure.Extensions;

/// <summary>
/// Методы расширения для регистрации зависимостей Infrastructure в контейнере DI.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует <see cref="ShopDbContext"/> с провайдером PostgreSQL
    /// и все репозитории Infrastructure.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="connectionString">Строка подключения к PostgreSQL.</param>
    /// <returns>Коллекция сервисов для цепочки вызовов.</returns>
    public static IServiceCollection AddPostgresql(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<ShopDbContext>(options =>
            options.UseNpgsql(connectionString));
        return services.AddRepositories();
    }

    /// <summary>
    /// Регистрирует <see cref="ShopDbContext"/> с провайдером Microsoft SQL Server
    /// и все репозитории Infrastructure.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="connectionString">Строка подключения к MSSQL.</param>
    /// <returns>Коллекция сервисов для цепочки вызовов.</returns>
    public static IServiceCollection AddMssql(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<ShopDbContext>(options =>
            options.UseSqlServer(connectionString));
        return services.AddRepositories();
    }

    /// <summary>
    /// Регистрирует репозитории из Infrastructure.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <returns>Коллекция сервисов для цепочки вызовов.</returns>
    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        return services;
    }
}
