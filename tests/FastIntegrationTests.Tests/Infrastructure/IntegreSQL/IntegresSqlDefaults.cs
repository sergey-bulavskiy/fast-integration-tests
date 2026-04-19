using FastIntegrationTests.Infrastructure.Data;
using MccSoft.IntegreSql.EF.DatabaseInitialization;

namespace FastIntegrationTests.Tests.Infrastructure.IntegreSQL;

/// <summary>
/// Общие настройки IntegreSQL для всех тест-проектов.
/// </summary>
internal static class IntegresSqlDefaults
{
    /// <summary>Параметры сидирования шаблонной БД магазина.</summary>
    internal static readonly DatabaseSeedingOptions<ShopDbContext> SeedingOptions =
        new(
            Name: "shop-default",
            SeedingFunction: async ctx => await ctx.Database.MigrateAsync(),
            DisableEnsureCreated: true,
            DbContextFactory: opts => new ShopDbContext(opts)
        );
}
