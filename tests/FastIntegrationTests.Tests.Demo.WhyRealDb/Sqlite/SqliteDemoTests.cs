namespace FastIntegrationTests.Tests.Demo.WhyRealDb.Sqlite;

/// <summary>
/// Демонстрация: EF Core SQLite — другой диалект и другая семантика, чем PostgreSQL.
/// Нативный LIKE регистронезависим (EF.Functions.Like), decimal не поддерживается в ORDER BY,
/// сортировка строк по BINARY-коллации, DateTime теряет Kind, нет функций вроде string_agg,
/// а схема из raw-SQL миграций (триггеры) при EnsureCreated() не создаётся.
/// Каждый тест намеренно КРАСНЫЙ.
/// Зелёные эквиваленты — в tests/FastIntegrationTests.Tests.IntegreSQL/.
/// </summary>
public class SqliteDemoTests : SqliteDemoBase
{
    /// <summary>
    /// (1) Что проверяем: нативный SQL LIKE ('%apple%') регистрозависим.
    /// (2) Postgres: LIKE регистрозависим — "Apple" НЕ совпадает с '%apple%' → 0 строк. Зелёный.
    /// (3) Что не так у SQLite: LIKE по умолчанию регистронезависим для ASCII — "Apple" совпадает с '%apple%'.
    /// (4) Почему красный: EF.Functions.Like транслируется в нативный LIKE без доп. коллации;
    ///     SQLite находит "Apple", Assert.Empty падает.
    /// (5) Зелёный эквивалент: тесты регистрозависимого поиска в Tests.IntegreSQL.
    /// НАБЛЮДЕНИЕ (риск): EF Core string.Contains транслирует в LIKE с COLLATE BINARY (регистрозависимо),
    ///     поэтому используется EF.Functions.Like для нативного LIKE без коллации.
    /// </summary>
    [Fact]
    [Trait("Category", "Demo")]
    public void Sqlite_LikeIsCaseInsensitive_DivergesFromPostgres()
    {
        using var context = CreateContext();
        context.Products.Add(new Product { Name = "Apple", Description = "", Price = 1m, CreatedAt = DateTime.UtcNow });
        context.SaveChanges();
        context.ChangeTracker.Clear();

        // EF.Functions.Like транслируется в нативный SQL LIKE без COLLATE-обёртки.
        // SQLite: LIKE '%apple%' регистронезависим → находит "Apple".
        // Postgres: LIKE '%apple%' регистрозависим → не находит "Apple".
        var found = context.Products.Where(p => EF.Functions.Like(p.Name, "%apple%")).ToList();

        // Postgres: LIKE регистрозависим → ничего не найдено; SQLite находит "Apple" → красный.
        Assert.Empty(found);
    }

    /// <summary>
    /// (1) Что проверяем: ORDER BY по decimal цене сортирует численно.
    /// (2) Postgres: numeric(18,2), ORDER BY Price ASC → 9.99 раньше 100.00. Зелёный.
    /// (3) Что не так у SQLite: decimal не поддерживается в ORDER BY EF Core SQLite —
    ///     провайдер выбрасывает NotSupportedException ещё на этапе трансляции запроса.
    /// (4) Почему красный: EF Core SQLite бросает NotSupportedException
    ///     «SQLite does not support expressions of type 'decimal' in ORDER BY clauses» → тест падает.
    /// (5) Зелёный эквивалент: тесты сортировки/сумм по цене в Tests.IntegreSQL.
    /// НАБЛЮДЕНИЕ (фактическое поведение, зафиксировано 2026-05-30): вместо текстовой мис-сортировки
    ///     EF Core SQLite выбрасывает NotSupportedException при трансляции ORDER BY decimal.
    ///     Ожидание из спеки («100.00 первым из-за лексикографии») не реализуется — EF запрос вообще
    ///     не выполняется. Это тоже демонстрирует несовместимость: Postgres поддерживает, SQLite — нет.
    /// </summary>
    [Fact]
    [Trait("Category", "Demo")]
    public void Sqlite_DecimalPrecision_OrderByDivergesFromPostgres()
    {
        using var context = CreateContext();
        context.Products.AddRange(
            new Product { Name = "Дорогой", Description = "", Price = 100.00m, CreatedAt = DateTime.UtcNow },
            new Product { Name = "Дешёвый", Description = "", Price = 9.99m, CreatedAt = DateTime.UtcNow });
        context.SaveChanges();
        context.ChangeTracker.Clear();

        // Postgres: численный ORDER BY работает → 9.99 первым. Зелёный.
        // SQLite: EF Core выбрасывает NotSupportedException при трансляции ORDER BY decimal → красный.
        // Фактическое сообщение: «SQLite does not support expressions of type 'decimal' in ORDER BY clauses.»
        var pricesAsc = context.Products.OrderBy(p => p.Price).Select(p => p.Price).ToList();

        Assert.Equal(9.99m, pricesAsc[0]);
    }

    /// <summary>
    /// (1) Что проверяем: ORDER BY name даёт словарный порядок ("apple" раньше "Zebra").
    /// (2) Postgres (коллация en_US/ICU, типичная для dev-окружения): словарный, регистронезависимый
    ///     порядок → "apple" &lt; "Zebra".
    /// (3) Что не так у SQLite: коллация по умолчанию BINARY — сравнение по кодам символов:
    ///     'Z'(90) &lt; 'a'(97) → "Zebra" раньше "apple".
    /// (4) Почему красный: первым приходит "Zebra", ассерт "apple" первым падает.
    /// (5) Зелёный эквивалент: тесты сортировки по имени в Tests.IntegreSQL.
    /// ВНИМАНИЕ: красный гарантирован (SQLite BINARY детерминирован). Предполагаемая Postgres-коллация —
    /// en_US/ICU; если PG-контейнер репозитория инициализирован с C/POSIX, словарный эквивалент будет иным —
    /// демо всё равно выполняется только на SQLite, так что красный сохраняется. Зафиксируй допущение.
    /// </summary>
    [Fact]
    [Trait("Category", "Demo")]
    public void Sqlite_StringCollation_OrderByDivergesFromPostgres()
    {
        using var context = CreateContext();
        context.Products.AddRange(
            new Product { Name = "Zebra", Description = "", Price = 1m, CreatedAt = DateTime.UtcNow },
            new Product { Name = "apple", Description = "", Price = 2m, CreatedAt = DateTime.UtcNow });
        context.SaveChanges();
        context.ChangeTracker.Clear();

        var names = context.Products.OrderBy(p => p.Name).Select(p => p.Name).ToList();

        // Postgres (en_US): "apple" первым; SQLite (BINARY): "Zebra" первым → красный.
        Assert.Equal("apple", names[0]);
    }

    /// <summary>
    /// (1) Что проверяем: DateTime с Kind=Utc сохраняется и читается как UTC.
    /// (2) Postgres: timestamp with time zone сохраняет момент времени, Npgsql читает Kind=Utc.
    /// (3) Что не так у SQLite: DateTime хранится как ISO-текст без зоны; при чтении Kind=Unspecified.
    /// (4) Почему красный: прочитанный Kind == Unspecified, ассерт Kind == Utc падает.
    /// (5) Зелёный эквивалент: тесты на CreatedAt в Tests.IntegreSQL.
    /// </summary>
    [Fact]
    [Trait("Category", "Demo")]
    public void Sqlite_TimestampTz_LosesDateTimeKind()
    {
        using var context = CreateContext();
        context.Products.Add(new Product { Name = "X", Description = "", Price = 1m, CreatedAt = DateTime.UtcNow });
        context.SaveChanges();
        context.ChangeTracker.Clear();

        var read = context.Products.AsNoTracking().Single();

        // Postgres: Kind восстанавливается как Utc; SQLite теряет зону → Unspecified → красный.
        Assert.Equal(DateTimeKind.Utc, read.CreatedAt.Kind);
    }

    /// <summary>
    /// (1) Что проверяем: агрегатная функция string_agg выполняется.
    /// (2) Postgres: string_agg существует и склеивает имена через разделитель. Зелёный.
    /// (3) Что не так у SQLite: функции string_agg нет (есть group_concat) — запрос не выполняется.
    /// (4) Почему красный: SqlQueryRaw бросает SqliteException «no such function: string_agg» → тест падает.
    /// (5) Зелёный эквивалент: отчётные/агрегатные запросы в Tests.IntegreSQL.
    /// </summary>
    [Fact]
    [Trait("Category", "Demo")]
    public void Sqlite_DoesNotSupport_StringAgg()
    {
        using var context = CreateContext();
        context.Products.AddRange(
            new Product { Name = "A", Description = "", Price = 1m, CreatedAt = DateTime.UtcNow },
            new Product { Name = "B", Description = "", Price = 2m, CreatedAt = DateTime.UtcNow });
        context.SaveChanges();

        // Postgres выполнил бы string_agg; SQLite бросает «no such function» → красный.
        var aggregated = context.Database
            .SqlQueryRaw<string>("SELECT string_agg(\"Name\", ',') AS \"Value\" FROM \"Products\"")
            .AsEnumerable()
            .First();

        Assert.False(string.IsNullOrEmpty(aggregated));
    }

    /// <summary>
    /// (1) Что проверяем: после изменения цены товара триггер записал строку в PriceHistory.
    /// (2) Postgres: миграция 20260416000011 создаёт таблицу PriceHistory и триггер
    ///     trg_products_price_change (AFTER UPDATE ON Products) — при смене Price появляется строка истории.
    /// (3) Что не так у SQLite: EnsureCreated() строит схему из EF-модели и НЕ применяет raw-SQL миграции —
    ///     таблицы PriceHistory и триггера не существует.
    /// (4) Почему красный: запрос к PriceHistory бросает SqliteException «no such table: PriceHistory» → тест падает.
    /// (5) Зелёный эквивалент: проверка истории цен на реальной БД в Tests.IntegreSQL.
    /// Урок: к фейку миграции не применяются — побочные эффекты триггеров отсутствуют.
    /// </summary>
    [Fact]
    [Trait("Category", "Demo")]
    public void Sqlite_MigrationsNotApplied_PriceHistoryTriggerMissing()
    {
        using var context = CreateContext();
        var product = new Product { Name = "X", Description = "", Price = 10m, CreatedAt = DateTime.UtcNow };
        context.Products.Add(product);
        context.SaveChanges();

        product.Price = 20m;
        context.SaveChanges();

        // Postgres: триггер записал 1 строку истории; в схеме SQLite таблицы нет → красный (no such table).
        var historyCount = context.Database
            .SqlQueryRaw<int>("SELECT COUNT(*) AS \"Value\" FROM \"PriceHistory\"")
            .AsEnumerable()
            .First();

        Assert.Equal(1, historyCount);
    }
}
