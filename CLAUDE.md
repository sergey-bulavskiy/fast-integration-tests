# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Рабочий процесс

- Фича-ветки мержатся в `main` через **squash merge** (`git merge --squash`): один коммит на фичу.
- `docs/benchmark-issues/` — локальные файлы, не отслеживаются git (в `.gitignore`).

## Инструменты разработки

При работе с C#-кодом в этом репозитории — всегда использовать LSP для C# и включать его по умолчанию в начале сессии.

**Исходники IntegreSQL.EF** (обёртка над IntegreSQL для EF Core): https://github.com/mccsoft/IntegreSql.EF

## Команды разработки

```bash
# Сборка всего решения
dotnet build

# Запуск сервиса
dotnet run --project src/FastIntegrationTests.WebApi

# Восстановить локальные инструменты (нужно один раз после клонирования)
dotnet tool restore

# Запустить PostgreSQL локально
docker-compose up postgres -d

# Добавить новую миграцию
dotnet ef migrations add <НазваниеМиграции> \
  --project src/FastIntegrationTests.Infrastructure \
  --startup-project src/FastIntegrationTests.WebApi \
  --output-dir Migrations

# Применить миграции
dotnet ef database update \
  --project src/FastIntegrationTests.Infrastructure \
  --startup-project src/FastIntegrationTests.WebApi
```

## Интеграционные тесты

```bash
# Запустить все тесты (требует запущенный Docker)
dotnet test

# Запустить один подход
dotnet test tests/FastIntegrationTests.Tests.IntegreSQL
dotnet test tests/FastIntegrationTests.Tests.Respawn
dotnet test tests/FastIntegrationTests.Tests.Testcontainers

# Запустить тесты отдельного класса (примеры — суффиксы Cr/Ud, подход без суффикса/Respawn/Container)
dotnet test tests/FastIntegrationTests.Tests.IntegreSQL --filter "FullyQualifiedName~ProductServiceCrTests"
dotnet test tests/FastIntegrationTests.Tests.Testcontainers --filter "FullyQualifiedName~OrdersApiUdContainerTests"
dotnet test tests/FastIntegrationTests.Tests.Respawn --filter "FullyQualifiedName~CategoryServiceUdRespawnTests"
```

### Как работают тесты

**Требование:** Docker должен быть запущен. Контейнеры PostgreSQL (и IntegreSQL для соответствующего подхода) поднимаются автоматически через Testcontainers.

**Инфраструктура скрыта:** тесты работают только через `IProductService` / `IOrderService` (сервисный уровень) или `HttpClient` (HTTP-уровень). Вся работа с БД — в базовых классах.

#### Три подхода к изоляции

**IntegreSQL** (`AppServiceTestBase` / `ComponentTestBase`):
- Один пара контейнеров (PostgreSQL + IntegreSQL) на весь процесс — `IntegresSqlContainerManager` (static Lazy).
- Миграции применяются **один раз** как шаблонная БД `"shop-default"`.
- Каждый тест получает **клон шаблона** и после завершения возвращает его в пул с пометкой «пересоздать» (`DropDatabaseOnRemove=true`).
- Тесты полностью изолированы — параллелизм внутри класса возможен.

**Respawn** (`RespawnServiceTestBase` / `RespawnApiTestBase`):
- Один контейнер PostgreSQL **на класс** (через `IClassFixture<RespawnFixture>`).
- Миграции применяются **один раз на класс** в `RespawnFixture.InitializeAsync()`.
- Между тестами — `TRUNCATE CASCADE` через Respawn, схема сохраняется.
- TestServer и HttpClient создаются **один раз на класс** и переиспользуются.
- Тесты внутри одного класса выполняются **последовательно** (общая БД).

**Testcontainers** (`ContainerServiceTestBase` / `ContainerApiTestBase`):
- Один контейнер PostgreSQL **на класс** (через `IClassFixture<ContainerFixture>`).
- Миграции применяются **один раз на класс**.
- Между тестами — пересоздание БД через `EnsureDeleted` + `MigrateAsync`.
- TestServer и HttpClient создаются **на каждый тест**.

#### Сравнение по ключевым параметрам

| | IntegreSQL | Respawn | Testcontainers |
|---|---|---|---|
| Контейнер | 1 на процесс | 1 на класс | 1 на класс |
| Миграции | 1 раз (весь процесс) | 1 раз (класс) | 1 раз (класс) |
| Сброс данных | возврат клона в пул (recreate) | TRUNCATE CASCADE | EnsureDeleted + Migrate |
| TestServer (API) | новый на каждый тест | 1 на класс | новый на каждый тест |
| Параллелизм внутри класса | да | нет | да |

### PowerShell скрипты для запуска тестов

В корне репозитория есть готовые скрипты с параметром `-Threads`:

```powershell
# Бизнес-тесты каждого подхода (по умолчанию: 4 потока)
.\run-integresql.ps1
.\run-testcontainers.ps1
.\run-respawn.ps1

# Переопределить параметры
.\run-integresql.ps1 -Threads 8
```

Каждый скрипт выводит итоговое время выполнения в формате `мм:сс.ммм`.

## Benchmark Runner

Консольный инструмент для сравнительного бенчмарка трёх подходов по трём сценариям. Результат — HTML отчёт с интерактивными Chart.js графиками (имя с таймстемпом, например `benchmark-results/report-20260425-143022.html`).

```bash
# Запуск с дефолтными параметрами (Docker должен быть запущен, ~1–2 часа)
dotnet run --project tools/BenchmarkRunner

# Переопределить дефолтные потоки и масштаб классов
dotnet run --project tools/BenchmarkRunner -- --threads 4 --scale 10
dotnet run --project tools/BenchmarkRunner -- -t 16 -s 50

# После завершения runner выводит точный путь к отчёту, например:
# Open: benchmark-results/report-20260425-143022.html
```

| Аргумент | По умолчанию | Применяется в |
|---|---|---|
| `--scale N` / `-s N` | 12 | Сценарии 1 и 3 (масштаб классов) |
| `--threads N` / `-t N` | 8 | Сценарии 1 и 2 (потоки не варьируются) |
| `--timeout N` | 50 | Таймаут одного прогона dotnet test (минуты) |

> **Важно:** константа `BaseTestCount` в `tools/BenchmarkRunner/Program.cs` — хардкод.
> При добавлении или удалении тест-методов обновить вручную.
> Актуальное значение: `dotnet test tests/FastIntegrationTests.Tests.IntegreSQL --list-tests 2>/dev/null | grep -c "::"`

### Три сценария

| Сценарий | Фиксируется | Варьируется |
|---|---|---|
| 1 — Влияние числа миграций | `--scale`, `--threads` | 17 / 42 / 67 / 92 / 117 миграций |
| 2 — Масштаб числа тестов | 117 миграций, `--threads` | ClassScale: 1, 5, 10, 20, 50 |
| 3 — Параллелизм | 117 миграций, `--scale` | потоков: 1, 2, 4, 8 |

Итого 42 точки данных (5+5+4 × 3 подхода). Перед Сценарием 1 выполняется warmup-прогон (IntegreSQL, ClassScale=1), результат не сохраняется. Benchmark-миграции скрываются и восстанавливаются автоматически в Сценарии 1 — репозиторий возвращается в исходное состояние.

### Benchmark-миграции

В репозитории закоммичены 100 benchmark-миграций (timestamp `20990101000001`–`20990101000100`) в папке `src/FastIntegrationTests.Infrastructure/Migrations/`. Итого 117 миграций — базовое состояние бенчмарка.

Два чередующихся типа с реальной нагрузкой на БД:

- **Нечётные** — `CREATE TABLE load_tmp_NNN` + INSERT 300 строк (~10–20 мс)
- **Чётные** — `DROP TABLE IF EXISTS load_tmp_{N-1}` (~2–5 мс)

После применения всех 100 миграций схема остаётся чистой (последняя чётная удаляет таблицу предыдущей нечётной). Год `2099` гарантирует сортировку после любых реальных миграций.

**Scenario 1** (влияние числа миграций) скрывает лишние benchmark-миграции в `Migrations/__hidden/` и восстанавливает их после каждой точки данных. Scenarios 2 и 3 работают со всеми 117 миграциями без манипуляций.

### Выходные файлы

- `benchmark-results/report-<timestamp>.html` — HTML отчёт: три линейных графика + stacked bar состава времени (gitignored)
- `benchmark-results/results-<timestamp>.json` — сырые данные; перезаписывается после каждой точки данных (gitignored)

Таймстемп формата `yyyyMMdd-HHmmss` фиксируется при старте `ReportGenerator` — оба файла одного прогона имеют одинаковый суффикс.

### Архитектура инструмента

```
tools/BenchmarkRunner/
├── Program.cs                  — оркестрация трёх сценариев
├── Models/                     — BenchmarkScenario, BenchmarkResult, BenchmarkReport
├── Runner/TestRunner.cs        — запуск dotnet test через Process, замер времени
├── Migrations/MigrationManager.cs — запись/удаление фейковых .cs миграций
└── Report/
    ├── ReportGenerator.cs      — сериализация JSON, инлайн в HTML шаблон
    └── report-template.html    — Chart.js шаблон с плейсхолдером /*INJECT_JSON*/
```

## Архитектура

Трёхслойная архитектура:

- **Application** — доменные сущности (`Entities/`), перечисления (`Enums/`), DTO (`DTOs/`), интерфейсы репозиториев и сервисов (`Interfaces/`), сервисы бизнес-логики (`Services/`), доменные исключения (`Exceptions/`). Не зависит от EF Core и конкретной СУБД.
- **Infrastructure** — реализация репозиториев через EF Core (`Repositories/`), `ShopDbContext` с конфигурациями (`Data/`), extension-методы регистрации DI (`Extensions/ServiceCollectionExtensions.cs`).
- **WebApi** — контроллеры (`Controllers/`), `Program.cs` с DI-конфигурацией, глобальная обработка ошибок (`Middleware/GlobalExceptionHandler.cs`).
- **Tests.Shared** (`tests/FastIntegrationTests.Tests.Shared/`) — общая инфраструктура для всех трёх подходов: `TestWebApplicationFactory` (ASP.NET Core тест-сервер с подменой строки подключения).
- **Tests.IntegreSQL** (`tests/FastIntegrationTests.Tests.IntegreSQL/`) — интеграционные тесты через IntegreSQL. Инфраструктура: `AppServiceTestBase`, `ComponentTestBase`, `IntegreSQL/` (менеджер контейнеров). Тест-классы в папках по сущностям: `Categories/`, `Customers/`, `Discounts/`, `Orders/`, `Products/`, `Reviews/`, `Suppliers/` — по 4 класса на сущность (`*CrTests`, `*UdTests` для сервисного и HTTP уровней).
- **Tests.Respawn** (`tests/FastIntegrationTests.Tests.Respawn/`) — интеграционные тесты через Respawn. Инфраструктура: `RespawnServiceTestBase`, `RespawnApiTestBase`, `RespawnFixture`, `RespawnApiFixture`. Та же структура тест-классов.
- **Tests.Testcontainers** (`tests/FastIntegrationTests.Tests.Testcontainers/`) — интеграционные тесты через Testcontainers. Инфраструктура: `ContainerServiceTestBase`, `ContainerApiTestBase`, `TestDbFactory`, `ContainerFixture`. Та же структура тест-классов.

## Локальная разработка

Файл `appsettings.Development.json` не хранится в репозитории. После клонирования скопировать шаблон:
```bash
cp src/FastIntegrationTests.WebApi/appsettings.Development.json.example src/FastIntegrationTests.WebApi/appsettings.Development.json
```
Затем заполнить строку подключения PostgreSQL.

## Соглашения

- Документация и комментарии на русском языке.
- Все публичные классы и методы — с XML-документацией (`/// <summary>`).
- Все async-методы с `CancellationToken ct` параметром обязательно документируются тегом `<param name="ct">Токен отмены операции.</param>`.
- Коммит после каждого логического шага.

## Архитектурные ограничения

- **Application не зависит от EF Core.** Проект `FastIntegrationTests.Application` не содержит ни одного `<PackageReference>` на EF Core или провайдеры БД. Добавление таких зависимостей — нарушение архитектуры.

## Идеи для развития бенчмарка

### ~~Разноплановый набор тестов для честного бенчмарка~~ ✓ Реализовано

> Спроектировано и запланировано в `docs/superpowers/specs/2026-04-25-heavy-tests-design.md` и `docs/superpowers/plans/2026-04-25-heavy-tests.md`.
> 168 новых тестов (84 класса × 2) добавляются в существующие классы; ~16–26 SQL-операций на тест с реалистичными цепочками create→update→verify→delete и накруткой БД.

### ~~Декомпозиция времени: миграции / сброс / бизнес-логика~~ ✓ Реализовано

> Спроектировано в `docs/superpowers/specs/2026-04-25-migration-timing-design.md`.
> Stacked bar chart в отчёте показывает состав времени при 17 и 117 миграциях.
> Инструментирование через `##BENCH[migration]=` и `##BENCH[reset]=` в stdout тест-инфраструктуры.

### Тесты, которые падают на InMemory и SQLite

Отдельный класс тестов для демонстрации того, зачем вообще нужна реальная БД.
Каждый тест должен проходить на PostgreSQL и падать (или давать неверный результат) на EF Core InMemory / SQLite.

**Ограничения целостности — InMemory их не проверяет:**
- Вставить `OrderItem` с несуществующим `ProductId` → InMemory пропускает, PostgreSQL бросает FK violation
- Вставить два товара с одинаковым именем если есть UNIQUE constraint → InMemory пропускает, PostgreSQL отказывает
- Удалить `Product` у которого есть `OrderItem` → InMemory пропускает cascade/restrict, PostgreSQL применяет

**Поведение транзакций — InMemory не откатывает:**
- Два параллельных теста пишут в одну строку → на InMemory нет конфликта, на PostgreSQL одна транзакция побеждает
- Rollback при исключении внутри транзакции → InMemory не гарантирует откат

**Диалект SQL — SQLite не поддерживает:**
- `ILIKE` (регистронезависимый поиск) — SQLite использует `LIKE` с другой семантикой
- `string_agg` / `array_agg` — отсутствуют в SQLite
- Точность `TIMESTAMP WITH TIME ZONE` — SQLite хранит как текст, теряет timezone
- `RETURNING` в INSERT (старые версии SQLite)

**Неявные различия которые не падают но дают неверный результат:**
- Сортировка строк: PostgreSQL чувствителен к collation, SQLite — нет; `ORDER BY name` может вернуть другой порядок
- `LIKE '%foo%'` на PostgreSQL чувствителен к регистру, на SQLite — нет по умолчанию
