using FastIntegrationTests.Infrastructure.Data;
using MccSoft.IntegreSql.EF.DatabaseInitialization;
using Npgsql;

namespace FastIntegrationTests.Tests.Infrastructure.IntegreSQL;

/// <summary>
/// Общие настройки IntegreSQL для всех тест-проектов.
/// </summary>
public static class IntegresSqlDefaults
{
    /// <summary>Параметры сидирования шаблонной БД магазина.</summary>
    public static readonly DatabaseSeedingOptions<ShopDbContext> SeedingOptions =
        new(
            Name: "shop-default",
            SeedingFunction: async ctx =>
            {
                await ctx.Database.MigrateAsync();
                // После миграций закрываем все pooled-соединения Npgsql к шаблонной БД.
                // Без этого IntegreSQL падает при CREATE DATABASE ... TEMPLATE с ошибкой
                // "source database is being accessed by other users".
                NpgsqlConnection.ClearAllPools();
            },
            DisableEnsureCreated: true,
            DbContextFactory: opts => new ShopDbContext(opts)
        );
}
