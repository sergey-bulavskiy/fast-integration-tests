# Демо: почему Mock / InMemory / SQLite не заменяют реальный PostgreSQL

Набор демо-тестов для доклада. Каждый тест показывает конкретную проблему, которая на
реальном PostgreSQL не возникает (или ловится), а на фейке проскакивает молча, даёт неверный
результат или маскирует запрос, который реальная БД отвергла бы.

## ⚠️ Эти тесты ДОЛЖНЫ быть КРАСНЫМИ

Красный на фейке — это и есть демонстрация. `dotnet test` вернёт ненулевой код, и это
ожидаемо. НЕ «чините» тесты, чтобы они зеленели — зелёный тест здесь означает, что фейк
случайно совпал с реальной БД, и демонстрация сломана.

Docker не нужен: всё работает в памяти, проект запускается мгновенно прямо на ноутбуке.

## Запуск

```bash
dotnet test tests/FastIntegrationTests.Tests.Demo.WhyRealDb
dotnet test tests/FastIntegrationTests.Tests.Demo.WhyRealDb --filter "FullyQualifiedName~Mock"
dotnet test tests/FastIntegrationTests.Tests.Demo.WhyRealDb --filter "FullyQualifiedName~InMemory"
dotnet test tests/FastIntegrationTests.Tests.Demo.WhyRealDb --filter "FullyQualifiedName~Sqlite"
```

## Как фейки строят схему

InMemory и SQLite поднимают настоящий `ShopDbContext` через `EnsureCreated()` — схема
собирается из EF-модели (конфигураций `IEntityTypeConfiguration`), а raw-SQL миграции
(триггеры, FTS, materialized views) НЕ применяются. Отсюда напрямую растёт SQLite-хит про
отсутствующий триггер `PriceHistory`.

## Карта «фейк × хит × что ломается»

| Фейк | Тест | Что ломается на фейке |
|---|---|---|
| Mock | `Mock_HasNoState_ReadAfterWriteIsLost` | Нет состояния между вызовами: Update «теряется» |
| Mock | `Mock_DoesNotExecuteInclude_ItemsContractDrifts` | Мок не выполняет JOIN/Include — форма запроса фиктивна |
| Mock | `Mock_DoesNotExecuteOrderBy_SortContractIsFake` | Мок не выполняет ORDER BY — сортировка не проверяется |
| Mock | `Mock_HasNoIdentity_CreatedEntityHasZeroId` | Нет identity БД: Id остаётся 0 |
| Mock | `Mock_HasNoState_DuplicateEmailSlipsThrough` | Нет данных: дубль email не отлавливается |
| InMemory | `InMemory_DoesNotEnforce_UniqueEmail` | UNIQUE-индекс не enforce'ится |
| InMemory | `InMemory_DoesNotEnforce_ForeignKey_OrderItemWithGhostProduct` | FK не enforce'ится |
| InMemory | `InMemory_DoesNotEnforce_RestrictOnDelete` | OnDelete(Restrict) не enforce'ится |
| InMemory | `InMemory_DoesNotRollback_Transaction` | Транзакции — no-op, нет отката |
| InMemory | `InMemory_DoesNotSupport_RawSql_Ilike` | FromSqlRaw / ILIKE не поддерживается |
| InMemory | `InMemory_SilentlyRuns_NonTranslatablePredicate` | Молча считает client-eval предикат, который PG отвергает |
| SQLite | `Sqlite_LikeIsCaseInsensitive_DivergesFromPostgres` | LIKE регистронезависим (PG — зависим); нужен EF.Functions.Like для воспроизведения |
| SQLite | `Sqlite_DecimalPrecision_OrderByDivergesFromPostgres` | ORDER BY decimal не поддерживается (NotSupportedException) |
| SQLite | `Sqlite_StringCollation_OrderByDivergesFromPostgres` | BINARY-коллация — другой порядок строк |
| SQLite | `Sqlite_TimestampTz_LosesDateTimeKind` | DateTime теряет Kind/таймзону |
| SQLite | `Sqlite_DoesNotSupport_StringAgg` | Нет функции string_agg |
| SQLite | `Sqlite_MigrationsNotApplied_PriceHistoryTriggerMissing` | raw-SQL миграции (триггер) к фейку не применяются |

## Изоляция

В репозитории нет `.sln`, BenchmarkRunner хардкодит четыре пути тест-проектов — этот проект
не подхватывается ни сборкой решения, ни бенчмарком, ни PowerShell-скриптами. Запускается
только явной командой `dotnet test tests/FastIntegrationTests.Tests.Demo.WhyRealDb`.
