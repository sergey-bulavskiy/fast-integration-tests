using Microsoft.Data.Sqlite;

namespace FastIntegrationTests.Tests.Demo.WhyRealDb.Sqlite;

/// <summary>
/// Базовый класс для демо-тестов на EF Core SQLite (<c>:memory:</c>).
/// Держит ОДНО открытое соединение на весь тест: in-memory БД SQLite живёт ровно
/// столько, сколько открыто соединение — закрыли соединение, БД исчезла.
/// Схема строится через <see cref="DatabaseFacade.EnsureCreated"/> из EF-модели,
/// БЕЗ raw-SQL миграций (триггеры, FTS, materialized views в схему не попадают).
/// </summary>
public abstract class SqliteDemoBase : IDisposable
{
    private readonly SqliteConnection _connection;

    /// <summary>Открывает in-memory соединение SQLite на время жизни теста.</summary>
    protected SqliteDemoBase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    /// <summary>
    /// Создаёт контекст на общем in-memory соединении. <see cref="DatabaseFacade.EnsureCreated"/>
    /// идемпотентен: при повторном вызове в том же тесте схема не пересоздаётся.
    /// В одном тесте достаточно одного контекста; если нужен «свежий» взгляд на данные из
    /// хранилища — вызови <c>context.ChangeTracker.Clear()</c> перед запросом.
    /// </summary>
    protected ShopDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ShopDbContext>()
            .UseSqlite(_connection)
            .Options;

        var context = new ShopDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    /// <summary>Закрывает соединение — in-memory БД уничтожается.</summary>
    public void Dispose() => _connection.Dispose();
}
