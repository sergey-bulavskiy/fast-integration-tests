# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

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
dotnet test tests/FastIntegrationTests.Tests

# Запустить один подход (bash / Git Bash)
dotnet test tests/FastIntegrationTests.Tests --filter "FullyQualifiedName~Tests.IntegreSQL"
dotnet test tests/FastIntegrationTests.Tests --filter "FullyQualifiedName~Tests.Testcontainers"
dotnet test tests/FastIntegrationTests.Tests --filter "FullyQualifiedName~Tests.Respawn"

# С повторами — сравнение производительности (bash)
TEST_REPEAT=19 dotnet test tests/FastIntegrationTests.Tests --filter "FullyQualifiedName~Tests.IntegreSQL"
TEST_REPEAT=19 dotnet test tests/FastIntegrationTests.Tests --filter "FullyQualifiedName~Tests.Testcontainers"
TEST_REPEAT=19 dotnet test tests/FastIntegrationTests.Tests --filter "FullyQualifiedName~Tests.Respawn"

# PowerShell
$env:TEST_REPEAT=19; dotnet test tests/FastIntegrationTests.Tests --filter "FullyQualifiedName~Tests.IntegreSQL"
$env:TEST_REPEAT=19; dotnet test tests/FastIntegrationTests.Tests --filter "FullyQualifiedName~Tests.Testcontainers"
$env:TEST_REPEAT=19; dotnet test tests/FastIntegrationTests.Tests --filter "FullyQualifiedName~Tests.Respawn"

# Переопределить количество потоков прямо из CLI
TEST_REPEAT=19 dotnet test tests/FastIntegrationTests.Tests --filter "FullyQualifiedName~Tests.IntegreSQL" -- xUnit.MaxParallelThreads=8

# Запустить тесты отдельного класса (примеры — суффиксы Cr/Ud, подход без суффикса/Respawn/Container)
dotnet test tests/FastIntegrationTests.Tests --filter "FullyQualifiedName~ProductServiceCrTests"
dotnet test tests/FastIntegrationTests.Tests --filter "FullyQualifiedName~OrdersApiUdContainerTests"
dotnet test tests/FastIntegrationTests.Tests --filter "FullyQualifiedName~CategoryServiceUdRespawnTests"
```

### Как работают тесты

**Требование:** Docker должен быть запущен. Контейнеры PostgreSQL (и IntegreSQL для соответствующего подхода) поднимаются автоматически через Testcontainers.

**Инфраструктура скрыта:** тесты работают только через `IProductService` / `IOrderService` (сервисный уровень) или `HttpClient` (HTTP-уровень). Вся работа с БД — в базовых классах.

#### Три подхода к изоляции

**IntegreSQL** (`AppServiceTestBase` / `ComponentTestBase`):
- Один пара контейнеров (PostgreSQL + IntegreSQL) на весь процесс — `IntegresSqlContainerManager` (static Lazy).
- Миграции применяются **один раз** как шаблонная БД `"shop-default"`.
- Каждый тест получает **клон шаблона** (~5 мс) и после завершения клон удаляется.
- Тесты полностью изолированы — параллелизм внутри класса возможен.

**Respawn** (`RespawnServiceTestBase` / `RespawnApiTestBase`):
- Один контейнер PostgreSQL **на класс** (через `IClassFixture<RespawnFixture>`).
- Миграции применяются **один раз на класс** в `RespawnFixture.InitializeAsync()`.
- Между тестами — `TRUNCATE CASCADE` через Respawn (~1 мс), схема сохраняется.
- TestServer и HttpClient создаются **один раз на класс** и переиспользуются.
- Тесты внутри одного класса выполняются **последовательно** (общая БД).

**Testcontainers** (`ContainerServiceTestBase` / `ContainerApiTestBase`):
- Один контейнер PostgreSQL **на класс** (через `IClassFixture<ContainerFixture>`).
- Миграции применяются **один раз на класс**.
- Между тестами — пересоздание БД через `EnsureDeleted` + `MigrateAsync` (~200 мс).
- TestServer и HttpClient создаются **на каждый тест**.

#### Сравнение по ключевым параметрам

| | IntegreSQL | Respawn | Testcontainers |
|---|---|---|---|
| Контейнер | 1 на процесс | 1 на класс | 1 на класс |
| Миграции | 1 раз (весь процесс) | 1 раз (класс) | 1 раз (класс) |
| Сброс данных | удаление клона | TRUNCATE ~1 мс | EnsureDeleted ~200 мс |
| TestServer (API) | новый на каждый тест | 1 на класс | новый на каждый тест |
| Параллелизм внутри класса | да | нет | да |

### PowerShell скрипты для запуска тестов

В корне репозитория есть готовые скрипты с параметрами `-Repeat` и `-Threads`:

```powershell
# Бизнес-тесты каждого подхода (по умолчанию: 5 повторов, 4 потока)
.\run-integresql.ps1
.\run-testcontainers.ps1
.\run-respawn.ps1

# Переопределить параметры
.\run-integresql.ps1 -Repeat 19 -Threads 8
```

Каждый скрипт выводит итоговое время выполнения в формате `мм:сс.ммм`.

## Benchmark Runner

Консольный инструмент для сравнительного бенчмарка трёх подходов по трём сценариям. Результат — `benchmark-results/report.html` с интерактивными Chart.js графиками.

```bash
# Запуск с дефолтными параметрами (Docker должен быть запущен, ~15–60 мин)
dotnet run --project tools/BenchmarkRunner

# Переопределить дефолтные потоки и повторы
dotnet run --project tools/BenchmarkRunner -- --threads 4 --repeat 10
dotnet run --project tools/BenchmarkRunner -- -t 16 -r 50

# Открыть отчёт после завершения
start benchmark-results/report.html   # Windows
open benchmark-results/report.html    # macOS
```

| Аргумент | По умолчанию | Применяется в |
|---|---|---|
| `--threads N` / `-t N` | 8 | Сценарии 1 и 2 (потоки не варьируются) |
| `--repeat N` / `-r N` | 38 | Сценарии 1 и 3 (повторы не варьируются) |

### Три сценария

| Сценарий | Фиксируется | Варьируется |
|---|---|---|
| 1 — Влияние числа миграций | `--repeat` (~2000 вызовов), `--threads` | 16 / 66 / 116 миграций |
| 2 — Масштаб числа тестов | 16 миграций, `--threads` | TEST_REPEAT: 1, 5, 10, 20, 50 |
| 3 — Параллелизм | 16 миграций, `--repeat` | потоков: 1, 2, 4, 8 |

Итого 36 точек данных (3+5+4 × 3 подхода). Перед Сценарием 1 выполняется warmup-прогон (IntegreSQL, TEST_REPEAT=1), результат не сохраняется. Фейковые миграции генерируются и удаляются автоматически — репозиторий возвращается в исходное состояние.

### Фейковые миграции

Два чередующихся типа, оба с реальной нагрузкой на БД:

- **Нечётные** — `CREATE TABLE benchmark_ref_NNN` + INSERT 300 строк (~10–20 мс)
- **Чётные** — `CREATE TABLE benchmark_lookup_NNN` + INSERT 150 строк (~5–10 мс)

> ALTER TABLE на `Products` не используется: таблица пустая в момент применения миграций (миграции выполняются до вставки тестовых данных), поэтому `UPDATE "Products"` был бы no-op.

### Выходные файлы

- `benchmark-results/report.html` — HTML отчёт с тремя линейными графиками (gitignored)
- `benchmark-results/results.json` — сырые данные для воспроизведения (gitignored)

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
- **Tests** (`tests/FastIntegrationTests.Tests/`) — интеграционные тесты. Инфраструктура в `Infrastructure/` (фикстуры, фабрики, базовые классы для трёх подходов). Тест-классы сгруппированы по подходу: `IntegreSQL/`, `Respawn/`, `Testcontainers/`, каждый содержит `Products/` и `Orders/`. Внутри каждой сущности тесты разбиты на `*CrTests` (GetAll, GetById, Create) и `*UdTests` (Update, Delete, статусные переходы) — по 8 классов на подход.

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

### Разноплановый набор тестов для честного бенчмарка

Текущий набор покрывает реальную бизнес-логику, но для бенчмарка важно включить тесты разного «веса», чтобы видеть как меняется соотношение бизнес-время/инфраструктура-время:

**Лёгкие (инфраструктура доминирует):**
- Чтение пустой таблицы — `GET /products` без данных, минимальный SQL
- 404 по несуществующему ID — контроллер возвращает ошибку без обращения к БД
- Создание одной сущности и немедленный возврат

**Средние (реалистичный микс):**
- Создание + чтение (write-then-read, проверка персистентности)
- Создание 5–10 сущностей, GetAll с сортировкой
- Обновление + повторное чтение

**Тяжёлые (бизнес-время сравнимо с инфраструктурой):**
- Полный жизненный цикл заказа (New → Confirmed → Shipped → Completed) — 4 write-операции
- Создание 20+ товаров, заказа с множеством позиций, агрегация суммы
- Каскадные проверки (FK, rollback при нарушении ограничений)

Соотношение ~30% лёгких / 50% средних / 20% тяжёлых даёт наиболее репрезентативную картину для сравнения подходов.

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
