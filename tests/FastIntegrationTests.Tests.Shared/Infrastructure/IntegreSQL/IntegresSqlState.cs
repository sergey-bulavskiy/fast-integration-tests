using MccSoft.IntegreSql.EF;

namespace FastIntegrationTests.Tests.Infrastructure.IntegreSQL;

/// <summary>
/// Готовое состояние IntegreSQL-инфраструктуры для получения тестовых БД.
/// </summary>
public sealed class IntegresSqlState
{
    /// <summary>Инициализатор баз данных через IntegreSQL.</summary>
    public NpgsqlDatabaseInitializer Initializer { get; }

    /// <summary>
    /// Создаёт новый экземпляр <see cref="IntegresSqlState"/>.
    /// </summary>
    /// <param name="initializer">Настроенный инициализатор БД.</param>
    public IntegresSqlState(NpgsqlDatabaseInitializer initializer)
    {
        Initializer = initializer;
    }
}
