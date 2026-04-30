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
dotnet test tests/FastIntegrationTests.Tests.TestcontainersShared

# Запустить тесты отдельного класса (примеры — IntegreSQL без суффикса, у Respawn/Testcontainers — Respawn/Container/Shared)
dotnet test tests/FastIntegrationTests.Tests.IntegreSQL --filter "FullyQualifiedName~ProductServiceTests"
dotnet test tests/FastIntegrationTests.Tests.Testcontainers --filter "FullyQualifiedName~OrdersApiContainerTests"
dotnet test tests/FastIntegrationTests.Tests.Respawn --filter "FullyQualifiedName~CategoryServiceRespawnTests"
dotnet test tests/FastIntegrationTests.Tests.TestcontainersShared --filter "FullyQualifiedName~CategoriesApiSharedTests"
```

### Как работают тесты

**Требование:** Docker должен быть запущен. Контейнеры PostgreSQL (и IntegreSQL для соответствующего подхода) поднимаются автоматически через Testcontainers.

**Инфраструктура скрыта:** тесты работают только через `IProductService` / `IOrderService` (сервисный уровень) или `HttpClient` (HTTP-уровень). Вся работа с БД — в базовых классах.

#### Четыре подхода к изоляции

**IntegreSQL** (`AppServiceTestBase` / `ComponentTestBase`):
- Один пара контейнеров (PostgreSQL + IntegreSQL) на весь процесс — `IntegresSqlContainerManager` (static Lazy).
- Миграции применяются **один раз** как шаблонная БД `"shop-default"`.
- Каждый тест получает **клон шаблона** и после завершения возвращает его в пул с пометкой «пересоздать» (`DropDatabaseOnRemove=true`).
- Тесты полностью изолированы — параллелизм внутри класса возможен.
- Data-каталог PostgreSQL смонтирован как tmpfs (RAM, ~1–1.5 GB при scale=50) — убирает диск из критического пути `CREATE DATABASE TEMPLATE` и делает бенчмарк нечувствительным к локальной дисковой подсистеме. Без этого на больших scale (s=50+) бенчмарк падает массой `Npgsql EndOfStream` из-за disk-троттлинга. Если RAM в обрез — закомментируй `WithTmpfsMount("/var/lib/postgresql/data")` в `IntegresSqlContainerManager.cs`.

**Respawn** (`RespawnServiceTestBase` / `RespawnApiTestBase`):
- Один контейнер PostgreSQL **на весь процесс** — `RespawnContainerManager` (static Lazy).
- Каждый класс создаёт отдельную БД (`CREATE DATABASE`) и дропает при завершении.
- Миграции применяются **один раз на класс** в `RespawnFixture.InitializeAsync()`.
- Между тестами — Respawn выполняет DELETE в детерминированном порядке по FK-зависимостям, схема сохраняется.
- TestServer и HttpClient создаются **один раз на класс** и переиспользуются.
- Тесты внутри одного класса выполняются **последовательно** (общая БД).

**Testcontainers** (`ContainerServiceTestBase` / `ContainerApiTestBase`):
- Один контейнер PostgreSQL **на класс** (через `IClassFixture<ContainerFixture>`).
- Каждый тест создаёт свою БД с уникальным именем `test_{guid}` через `TestDbFactory.CreateAsync` и применяет `MigrateAsync`. То есть **миграции на каждый тест**.
- В `DisposeAsync` вызывается `EnsureDeletedAsync` — БД дропается.
- TestServer и HttpClient создаются **на каждый тест**.

**TestcontainersShared** (`SharedServiceTestBase` / `SharedApiTestBase`):
- Один контейнер PostgreSQL **на весь процесс** — `SharedContainerManager` (static Lazy). Нет `IClassFixture`.
- Каждый тест создаёт свою БД `test_{guid}` через `SharedDbHandle.CreateAndMigrateAsync` и применяет `MigrateAsync`. То есть **миграции на каждый тест**.
- В `DisposeAsync` — `NpgsqlConnection.ClearPool` + `DROP DATABASE` через admin-соединение.
- TestServer и HttpClient создаются **на каждый тест**.

#### Сравнение по ключевым параметрам

| | IntegreSQL | Respawn | Testcontainers | TestcontainersShared |
|---|---|---|---|---|
| Контейнер | 1 на процесс (PG + IntegreSQL) | 1 на процесс | 1 на класс | 1 на процесс |
| Миграции | 1 раз на процесс | 1 раз на класс | **на каждый тест** | **на каждый тест** |
| Сброс данных | возврат клона в пул (recreate) | DELETE по FK-порядку | новая БД `test_{guid}` + `MigrateAsync`, потом `EnsureDeleted` | новая БД `test_{guid}` + `MigrateAsync`, потом `ClearPool` + `DROP DATABASE` |
| TestServer (API) | новый на каждый тест | 1 на класс | новый на каждый тест | новый на каждый тест |
| Параллелизм внутри класса | да | нет | да | да |

### PowerShell скрипты для запуска тестов

В корне репозитория есть готовые скрипты с параметром `-Threads`:

```powershell
# Бизнес-тесты каждого подхода (по умолчанию: 4 потока)
.\run-integresql.ps1
.\run-testcontainers.ps1
.\run-testcontainers-shared.ps1
.\run-respawn.ps1

# Переопределить параметры
.\run-integresql.ps1 -Threads 8
```

Каждый скрипт выводит итоговое время выполнения в формате `мм:сс.ммм`.

## Benchmark Runner

Консольный инструмент для сравнительного бенчмарка четырёх подходов по трём сценариям. Результат — HTML отчёт с интерактивными Chart.js графиками (имя с таймстемпом, например `benchmark-results/report-20260425-143022.html`).

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
| `--cooldown N` / `-c N` | 8 | Все сценарии (пауза перед каждым `dotnet test`, кроме первого) |

> **Важно:** константа `BaseTestCount` в `tools/BenchmarkRunner/Program.cs` — хардкод.
> При добавлении или удалении тест-методов обновить вручную.
> Актуальное значение: `dotnet test tests/FastIntegrationTests.Tests.IntegreSQL --list-tests 2>/dev/null | grep -c "FastIntegrationTests.Tests.IntegreSQL\."`

### Три сценария

| Сценарий | Фиксируется | Варьируется |
|---|---|---|
| 1 — Влияние числа миграций | `--scale`, `--threads` | 17 / 42 / 67 / 92 / 117 миграций |
| 2 — Масштаб числа тестов | 117 миграций, `--threads` | ClassScale: 1, 5, 10, 20, 50 |
| 3 — Параллелизм | 117 миграций, `--scale` | потоков: 1, 2, 4, 8 |

Итого 56 точек данных (5+5+4 × 4 подхода). Перед Сценарием 1 выполняется warmup-прогон (IntegreSQL, ClassScale=1), результат не сохраняется. Benchmark-миграции скрываются и восстанавливаются автоматически в Сценарии 1 — репозиторий возвращается в исходное состояние.

### Benchmark-миграции

В репозитории закоммичены 100 benchmark-миграций (timestamp `20990101000001`–`20990101000100`) в папке `src/FastIntegrationTests.Infrastructure/Migrations/`. Итого 117 миграций — базовое состояние бенчмарка.

Два чередующихся типа с реальной нагрузкой на БД:

- **Нечётные** — `CREATE TABLE load_tmp_NNN` + INSERT 300 строк (~10–20 мс)
- **Чётные** — `DROP TABLE IF EXISTS load_tmp_{N-1}` (~2–5 мс)

После применения всех 100 миграций схема остаётся чистой (последняя чётная удаляет таблицу предыдущей нечётной). Год `2099` гарантирует сортировку после любых реальных миграций.

**Сценарий 1** (влияние числа миграций) скрывает лишние benchmark-миграции в `Migrations/__hidden/` и восстанавливает их после каждой точки данных. Сценарии 2 и 3 работают со всеми 117 миграциями без манипуляций.

### Выходные файлы

- `benchmark-results/report-<timestamp>.html` — HTML отчёт: три линейных графика сравнения подходов + аналитические stacked bar графики (gitignored)
- `benchmark-results/results-<timestamp>.json` — сырые данные; перезаписывается после каждой точки данных (gitignored)

Таймстемп формата `yyyyMMdd-HHmmss` фиксируется при старте `ReportGenerator` — оба файла одного прогона имеют одинаковый суффикс.

### Архитектура инструмента

```
tools/BenchmarkRunner/
├── Program.cs                  — оркестрация трёх сценариев
├── Models/                     — BenchmarkScenario, BenchmarkResult, BenchmarkReport
├── Runner/TestRunner.cs        — запуск dotnet test через Process, замер времени
├── Migrations/MigrationManager.cs — скрытие/восстановление benchmark-миграций для Сценария 1
└── Report/
    ├── ReportGenerator.cs      — сериализация JSON, инлайн в HTML шаблон
    └── report-template.html    — Chart.js шаблон с плейсхолдером /*INJECT_JSON*/
```

## Архитектура

Трёхслойная архитектура:

- **Application** — доменные сущности (`Entities/`), перечисления (`Enums/`), DTO (`DTOs/`), интерфейсы репозиториев и сервисов (`Interfaces/`), сервисы бизнес-логики (`Services/`), доменные исключения (`Exceptions/`). Не зависит от EF Core и конкретной СУБД.
- **Infrastructure** — реализация репозиториев через EF Core (`Repositories/`), `ShopDbContext` с конфигурациями (`Data/`), extension-методы регистрации DI (`Extensions/ServiceCollectionExtensions.cs`).
- **WebApi** — контроллеры (`Controllers/`), `Program.cs` с DI-конфигурацией, глобальная обработка ошибок (`Middleware/GlobalExceptionHandler.cs`).
- **Tests.Shared** (`tests/FastIntegrationTests.Tests.Shared/`) — общая инфраструктура для всех подходов: `TestWebApplicationFactory` (ASP.NET Core тест-сервер с подменой строки подключения), `IntegresSqlContainerManager` + `IntegresSqlDefaults` + `IntegresSqlState` (общая IntegreSQL-инфра, переиспользуется xUnit- и NUnit-проектами).
- **Tests.IntegreSQL** (`tests/FastIntegrationTests.Tests.IntegreSQL/`) — интеграционные тесты через IntegreSQL (~195 тестов). Инфраструктура: `AppServiceTestBase`, `ComponentTestBase` (общий `IntegresSqlContainerManager` + `IntegresSqlDefaults` + `IntegresSqlState` живут в `Tests.Shared`). Тест-классы в папках по сущностям (`Categories/`, `Customers/`, `Discounts/`, `Orders/`, `Products/`, `Reviews/`, `Suppliers/`) — по 2 класса на сущность: `<Entity>ServiceTests` (сервисный уровень) и `<Entity>sApiTests` (HTTP-уровень). Итого 14 базовых классов на проект.
- **Tests.Respawn** (`tests/FastIntegrationTests.Tests.Respawn/`) — интеграционные тесты через Respawn (~195 тестов). Инфраструктура: `RespawnServiceTestBase`, `RespawnApiTestBase`, `RespawnFixture`, `RespawnApiFixture`. Те же 7 папок и 14 классов с суффиксом `Respawn`: `<Entity>ServiceRespawnTests`, `<Entity>sApiRespawnTests`.
- **Tests.Testcontainers** (`tests/FastIntegrationTests.Tests.Testcontainers/`) — интеграционные тесты через Testcontainers (~195 тестов). Инфраструктура: `ServiceTestBase`, `ApiTestBase` (общая логика), `ContainerServiceTestBase`, `ContainerApiTestBase` (объявляют `IClassFixture<ContainerFixture>`), `ContainerFixture`, `TestDbFactory`. Те же 7 папок и 14 классов с суффиксом `Container`: `<Entity>ServiceContainerTests`, `<Entity>sApiContainerTests`.
- **Tests.TestcontainersShared** (`tests/FastIntegrationTests.Tests.TestcontainersShared/`) — интеграционные тесты через Testcontainers с контейнером на процесс (~195 тестов). Инфраструктура: `SharedContainerManager` (static Lazy, контейнер на процесс), `SharedDbHandle` (lifecycle одной БД: create+migrate/drop), `SharedServiceTestBase`, `SharedApiTestBase`. Те же 7 папок и 14 классов с суффиксом `Shared`: `<Entity>ServiceSharedTests`, `<Entity>sApiSharedTests`.
- **Tests.NUnit.IntegreSQL** (`tests/FastIntegrationTests.Tests.NUnit.IntegreSQL/`) — учебный мини-проект для команд на NUnit. Содержит `ProductServiceTests` и `ProductsApiTests` (~25 тестов), демонстрирующие маппинг xUnit-инфраструктуры IntegreSQL на NUnit: `IAsyncLifetime` → `[SetUp]` / `[TearDown]`, constraint-model ассерты (`Assert.That(value, Is.EqualTo(...))`), `[Parallelizable]` + `LevelOfParallelism` на уровне assembly. В BenchmarkRunner и в PowerShell-скрипты не включён — это пример, а не альтернативный стек.

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

### Инфраструктура для NUnit-тестов

Сейчас все четыре подхода (IntegreSQL / Respawn / Testcontainers / TestcontainersShared) реализованы только для xUnit. У NUnit другая модель фикстур и параллелизма — стоит сделать параллельную инфраструктуру, чтобы:

- Показать на докладе, что подходы фреймворк-агностичные — это не свойство xUnit.
- Сравнить накладные расходы фреймворков на одинаковом наборе тестов (xUnit vs NUnit на тех же сценариях бенчмарка).
- Дать готовые шаблоны командам, которые сидят на NUnit и не хотят мигрировать.

**Что адаптировать:**
- Process-level инициализация (IntegreSQL `static Lazy`) → `[SetUpFixture]` с `[OneTimeSetUp]`.
- Class-level фикстуры (Respawn, Testcontainers) → `[OneTimeSetUp]` / `[OneTimeTearDown]` внутри тест-класса.
- Per-test reset (Respawn `ResetAsync`, IntegreSQL clone) → `[SetUp]` / `[TearDown]`.
- Параллелизм: `[Parallelizable(ParallelScope.All)]` + `LevelOfParallelism` в `AssemblyInfo`.
- `TestWebApplicationFactory` фреймворк-агностичен — переиспользуется как есть.

**Что добавить в бенчмарк:** ещё три проекта (`*.Tests.NUnit.IntegreSQL/Respawn/Testcontainers`) и три PowerShell-скрипта; в `BenchmarkRunner` — отдельная серия точек данных или отдельный сценарий «xUnit vs NUnit».

### Пересмотреть логику warmup в BenchmarkRunner

Сейчас перед Сценарием 1 запускается warmup по всем 4 подходам. Для IntegreSQL и Respawn это имеет смысл: прогрев JIT и Docker-образов (pull, если ещё нет) даёт чистую первую точку данных. Но для Testcontainers и TestcontainersShared каждый `dotnet test` — новый процесс с новым контейнером, который поднимается и сносится с нуля. Warmup-прогон ничего не прогревает для следующего процесса, поэтому первая реальная точка данных (m=17) всё равно будет «холодной» — так же, как если бы warmup не было.

Варианты для анализа:
- Убрать warmup для Testcontainers/TestcontainersShared совсем — первая точка всё равно холодная.
- Сделать warmup только для IntegreSQL и Respawn (которые держат контейнер на весь процесс runner'а — warmup им не поможет тоже, контейнер в тестах поднимается в отдельном `dotnet test` процессе).
- Понять, что вообще даёт warmup: возможно, он нужен только для прогрева Docker image cache, и тогда достаточно одного прогона любого подхода (первым идёт IntegreSQL — он уже всё прогреет для остальных через общий Docker daemon).
- Разобраться, нет ли двойного эффекта: `WaitForRyukToStop` + cooldown уже добавляют паузу после каждого прогона, а warmup — это дополнительный «нулевой» прогон сверху.
