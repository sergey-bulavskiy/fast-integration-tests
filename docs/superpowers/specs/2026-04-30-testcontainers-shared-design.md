# TestcontainersShared: общий контейнер на процесс + БД на тест

## Цель

Добавить четвёртый подход к изоляции — `Tests.TestcontainersShared` — рядом с существующими IntegreSQL / Respawn / Testcontainers. Старая инфраструктура `Tests.Testcontainers` остаётся неизменной для сравнения.

Идея:
- **Контейнер** PostgreSQL поднимается **один раз на процесс** (`static Lazy<Task<PostgreSqlContainer>>`, как в `RespawnContainerManager`).
- **БД** создаётся, мигрируется и дропается **на каждый тест** (как в текущем Testcontainers через `CREATE DATABASE` / `DROP DATABASE`).
- Перед `DROP` явно вызывается `NpgsqlConnection.ClearPool` — пользователь хотел увидеть освобождение пула шагом отдельно.

Это даёт честную оценку «сколько стоит контейнер на класс vs на процесс» — всё остальное идентично текущему Testcontainers.

В рамках этой задачи новый подход **не подключается к BenchmarkRunner**. Запускается отдельным PowerShell-скриптом и через `dotnet test`. Подключение к бенчмарку — отдельная будущая задача (упомянуто в `CLAUDE.md`).

---

## Архитектура

### Структура нового проекта

```
tests/FastIntegrationTests.Tests.TestcontainersShared/
├── FastIntegrationTests.Tests.TestcontainersShared.csproj
├── GlobalUsings.cs
├── xunit.runner.json
├── Infrastructure/
│   ├── SharedContainerManager.cs        — static Lazy контейнера
│   ├── SharedDbHandle.cs                 — internal хелпер CREATE/DROP
│   └── Base/
│       ├── SharedServiceTestBase.cs     — service-уровень
│       └── SharedApiTestBase.cs         — API-уровень
├── Categories/
│   ├── CategoryServiceSharedTests.cs
│   └── CategoriesApiSharedTests.cs
├── Customers/   Discounts/   Orders/   Products/   Reviews/   Suppliers/
                                                                (по 2 файла на папку)
```

Итого:
- 3 файла инфраструктуры
- 14 файлов тестов (7 папок × 2 файла) — точная копия тел из `Tests.Testcontainers`, отличается только базовый класс и суффикс имени

### Зависимости (.csproj)

Идентично `Tests.Testcontainers`:
- `ProjectReference`: `Application`, `Infrastructure`, `WebApi`, `Tests.Shared`
- `PackageReference`:
  - `Testcontainers.PostgreSql 4.4.0`
  - `Microsoft.NET.Test.Sdk 17.12.0`
  - `xunit.runner.visualstudio 2.8.2`
  - `coverlet.collector 6.0.4`

### Регистрация

- В `FastIntegrationTests.slnx` добавить новый `<Project Path="..." />` в группу `/tests/`.
- В `BenchmarkRunner` **ничего не менять** (сознательно, см. «Не интегрируется в бенчмарк»).
- В корень репозитория добавить `run-testcontainers-shared.ps1` — клон `run-testcontainers.ps1`.

---

## Компоненты

### `Infrastructure/SharedContainerManager.cs`

Клон `RespawnContainerManager`, переименованный. Один контейнер `postgres:16-alpine` на весь процесс. Лог `##BENCH[container]=` пишется один раз при первом обращении.

```csharp
namespace FastIntegrationTests.Tests.Infrastructure;

public static class SharedContainerManager
{
    private static readonly Lazy<Task<PostgreSqlContainer>> _container =
        new(() => StartAsync(), LazyThreadSafetyMode.ExecutionAndPublication);

    public static Task<PostgreSqlContainer> GetContainerAsync() => _container.Value;

    private static async Task<PostgreSqlContainer> StartAsync()
    {
        // Ryuk от предыдущего dotnet test (или soak'а) мог не успеть дочистить сеть/контейнеры.
        // На быстрых машинах Docker иначе переиспользует IP до того, как iptables очистит правила.
        await Task.Delay(TimeSpan.FromSeconds(10));

        var container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithCommand(
                // 195 тестов × scale=50 = ~9750 уникальных connection strings → ~9750 пулов
                // в одном процессе. Дефолт max_connections=100 заведомо мало.
                "-c", "max_connections=500",
                "-c", "fsync=off",
                "-c", "synchronous_commit=off",
                "-c", "full_page_writes=off",
                "-c", "shared_buffers=128MB"
            )
            .Build();

        var sw = Stopwatch.StartNew();
        await container.StartAsync();
        sw.Stop();
        BenchmarkLogger.Write("container", sw.ElapsedMilliseconds);

        // Новому Ryuk нужно успеть подняться, иначе первые тесты упрутся в незавершённый init.
        await Task.Delay(TimeSpan.FromSeconds(10));

        return container;
    }
}
```

Контейнер не диспозим явно — Ryuk-агент уберёт его при exit'е процесса (то же поведение, что в Respawn и IntegreSQL).

### `Infrastructure/SharedDbHandle.cs` (internal)

Чтобы не дублировать ~25 строк lifecycle между `SharedServiceTestBase` и `SharedApiTestBase`, выделяем internal хелпер с двумя методами: `CreateAsync` и `DropAsync`. Хелпер хранит `_connectionString` и `_dbName`.

```csharp
namespace FastIntegrationTests.Tests.Infrastructure;

internal sealed class SharedDbHandle
{
    public string ConnectionString { get; private set; } = null!;
    private string _dbName = null!;

    public async Task CreateAndMigrateAsync()
    {
        var container = await SharedContainerManager.GetContainerAsync();
        _dbName = $"test_{Guid.NewGuid():N}";
        var adminCs = container.GetConnectionString();

        await using (var admin = new NpgsqlConnection(adminCs))
        {
            await admin.OpenAsync();
            await using var cmd = admin.CreateCommand();
            cmd.CommandText = $"CREATE DATABASE \"{_dbName}\"";
            await cmd.ExecuteNonQueryAsync();
        }

        ConnectionString = new NpgsqlConnectionStringBuilder(adminCs) { Database = _dbName }
            .ConnectionString;

        var options = new DbContextOptionsBuilder<ShopDbContext>()
            .UseNpgsql(ConnectionString).Options;
        await using var ctx = new ShopDbContext(options);

        var sw = Stopwatch.StartNew();
        try
        {
            await ctx.Database.MigrateAsync();
        }
        catch
        {
            // Best-effort cleanup — БД создана, но миграция упала. Без cleanup
            // она останется висеть в контейнере (DisposeAsync xUnit не вызовет).
            await TryDropAsync();
            throw;
        }
        sw.Stop();
        BenchmarkLogger.Write("migration", sw.ElapsedMilliseconds);
    }

    public async Task DropAsync()
    {
        var sw = Stopwatch.StartNew();

        // 1. Освободить пул — без этого DROP DATABASE упадёт с
        //    "database is being accessed by other users" из-за idle-соединений.
        await using (var conn = new NpgsqlConnection(ConnectionString))
            NpgsqlConnection.ClearPool(conn);

        // 2. DROP DATABASE через admin-соединение
        var container = await SharedContainerManager.GetContainerAsync();
        await using var admin = new NpgsqlConnection(container.GetConnectionString());
        await admin.OpenAsync();
        await using var cmd = admin.CreateCommand();
        cmd.CommandText = $"DROP DATABASE IF EXISTS \"{_dbName}\"";
        await cmd.ExecuteNonQueryAsync();

        sw.Stop();
        BenchmarkLogger.Write("reset", sw.ElapsedMilliseconds);
    }

    private async Task TryDropAsync()
    {
        try { await DropAsync(); } catch { /* swallow — cleanup best-effort */ }
    }
}
```

### `Infrastructure/Base/SharedServiceTestBase.cs`

```csharp
namespace FastIntegrationTests.Tests.Infrastructure.Base;

public abstract class SharedServiceTestBase : IAsyncLifetime
{
    private readonly SharedDbHandle _db = new();

    /// <summary>Контекст тестовой БД. Доступен после InitializeAsync.</summary>
    protected ShopDbContext Context { get; private set; } = null!;

    public virtual async Task InitializeAsync()
    {
        await _db.CreateAndMigrateAsync();
        Context = new ShopDbContext(new DbContextOptionsBuilder<ShopDbContext>()
            .UseNpgsql(_db.ConnectionString).Options);
    }

    public virtual async Task DisposeAsync()
    {
        await Context.DisposeAsync();
        await _db.DropAsync();
    }
}
```

### `Infrastructure/Base/SharedApiTestBase.cs`

Не наследуется от `SharedServiceTestBase` — у API-тестов нет смысла светить `Context`. Структурно как `ApiTestBase` из текущего Testcontainers.

```csharp
namespace FastIntegrationTests.Tests.Infrastructure.Base;

public abstract class SharedApiTestBase : IAsyncLifetime
{
    private readonly SharedDbHandle _db = new();
    private TestWebApplicationFactory _factory = null!;

    /// <summary>HTTP-клиент для обращений к тестируемому API.</summary>
    protected HttpClient Client { get; private set; } = null!;

    public virtual async Task InitializeAsync()
    {
        await _db.CreateAndMigrateAsync();
        _factory = new TestWebApplicationFactory(_db.ConnectionString);
        Client = _factory.CreateClient();
    }

    public virtual async Task DisposeAsync()
    {
        Client?.Dispose();
        if (_factory is not null)
            // PhysicalFilesWatcher внутри WebApplicationFactory может бросить NRE
            // при диспозе под высокой параллельностью — баг в ASP.NET Core FileSystemWatcher.
            try { await _factory.DisposeAsync(); } catch (NullReferenceException) { }
        await _db.DropAsync();
    }
}
```

### Тест-классы

14 файлов — точная копия тел из `Tests.Testcontainers`. Отличия:
- namespace: `FastIntegrationTests.Tests.TestcontainersShared.<Entity>`
- имя класса: `<Entity>ServiceSharedTests` / `<Entity>sApiSharedTests`
- наследование: `: SharedServiceTestBase` / `: SharedApiTestBase` (вместо `IClassFixture<ContainerFixture>`)
- ctor с `ContainerFixture` удаляется
- `InitializeAsync` начинает с `await base.InitializeAsync()`, дальше инициализация SUT через `Context`

Пример (`Categories/CategoryServiceSharedTests.cs`):

```csharp
namespace FastIntegrationTests.Tests.TestcontainersShared.Categories;

public class CategoryServiceSharedTests : SharedServiceTestBase
{
    private ICategoryService Sut = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        Sut = new CategoryService(new CategoryRepository(Context));
    }

    [Fact]
    public async Task GetAllAsync_WhenNoCategories_ReturnsEmptyList() { ... }

    // ... все [Fact]-методы из CategoryServiceContainerTests, тела один-в-один
}
```

API-классы — аналогично, через `Client`.

---

## Лайфцикл

### Старт процесса (`dotnet test`)

1. xUnit обнаруживает классы и распределяет по collection'ам (одна collection на класс — дефолт).
2. Первый тест в любом классе вызывает `SharedContainerManager.GetContainerAsync()`. `Lazy<Task<…>>` гарантирует один старт; параллельные вызовы ждут готовности первого.
3. Контейнер `postgres:16-alpine` стартует один раз. `##BENCH[container]=` пишется один раз.

### Один тест

```
Init:    GetContainerAsync (cached, ~0мс)
       → CREATE DATABASE "test_{guid}"     (~5–20 мс)
       → new ShopDbContext
       → MigrateAsync                       (~50–800 мс, BENCH[migration])
Run:     [Fact] body
Dispose: Context.DisposeAsync               (~1 мс)
       → ClearPool                          (~1 мс)
       → DROP DATABASE                      (~10–30 мс, BENCH[reset])
```

### Параллелизм

- xUnit с `parallelizeTestCollections=true, threads=8` запускает до 8 классов одновременно.
- Внутри одного класса тесты идут последовательно (xUnit-дефолт).
- `Guid.NewGuid()` исключает коллизии имён БД.
- Один контейнер на всех → `max_connections=500` обязательно.

### Завершение

Контейнер не останавливается явно. Ryuk убивает его и все остаточные БД при exit'е процесса. `TestRunner.WaitForRyukToStop()` (если в будущем подключим к BenchmarkRunner) уже умеет ждать ухода Ryuk перед следующим прогоном.

---

## Edge Cases

| # | Случай | Решение |
|---|--------|---------|
| 1 | `MigrateAsync` падает в `InitializeAsync` | xUnit не вызывает `DisposeAsync`. `SharedDbHandle.CreateAndMigrateAsync` оборачивает миграцию в `try`/`catch`, в `catch` делает best-effort `DROP DATABASE` и перебрасывает исключение. |
| 2 | `DROP DATABASE` падает в `DisposeAsync` | Не пробрасываем — исключение из `DisposeAsync` ломает отчёт xUnit и скрывает реальные failure'ы тестов. БД останется до завершения процесса; Ryuk зачистит. |
| 3 | Гонка на первом `GetContainerAsync` | `Lazy<Task<…>>` + `ExecutionAndPublication` — стандартное решение. Если первый старт упал, `Lazy` запомнит провал и все последующие тесты упадут с тем же исключением. Намеренно. |
| 4 | Утечка контейнера при KILL процесса | Ryuk-агент Testcontainers уберёт. Ничего своего не делаем. |
| 5 | 20 секунд пауз в `StartAsync` | Не масштабируется — Lazy → один старт на процесс. На полном прогоне overhead однократный (в отличие от текущего Testcontainers, где 20с × N классов). |
| 6 | `scale=50` создаёт ~9750 уникальных connection strings | `ClearPool` per-test обязателен. Без него Postgres получает «too many clients» даже при `max_connections=500`. |
| 7 | Тест переопределяет `InitializeAsync` без `await base.InitializeAsync()` | Шаблон такой же, как в `RespawnServiceTestBase` — обязательный `await base.InitializeAsync()` первой строкой. Соглашение, не enforced код. |

---

## Не интегрируется в BenchmarkRunner

В рамках этой задачи новый проект **не** подключается к `tools/BenchmarkRunner`. Причина: пользователь хочет сначала вручную сравнить через PowerShell и только потом, если подход себя оправдает, включать в HTML-отчёт.

Что нужно будет сделать при будущем подключении (один абзац в `CLAUDE.md`):

1. В `tools/BenchmarkRunner/Program.cs` — добавить `"TestcontainersShared"` в массив `approaches` (4 элемента вместо 3).
2. В `tools/BenchmarkRunner/Runner/TestRunner.cs:Build()` — добавить путь `tests/FastIntegrationTests.Tests.TestcontainersShared` в массив `projects`.
3. В `tools/BenchmarkRunner/Scale/ClassScaleManager.cs` — добавить путь в `_testProjectPaths`.
4. Перепроверить `BaseTestCount` (если число тестов меняется).

Все три места уже содержат прецеденты для трёх существующих подходов — копируется по образцу.

---

## Документация в CLAUDE.md

Точечные правки в существующем `CLAUDE.md`:

### 1. Раздел «Интеграционные тесты» → команды

Добавить:
```bash
dotnet test tests/FastIntegrationTests.Tests.TestcontainersShared
dotnet test tests/FastIntegrationTests.Tests.TestcontainersShared --filter "FullyQualifiedName~CategoryServiceSharedTests"
```

### 2. Раздел «Три подхода к изоляции» → переименовать в «Четыре подхода»

Дописать блок:

> **TestcontainersShared** (`SharedServiceTestBase` / `SharedApiTestBase`):
> - Один контейнер PostgreSQL **на весь процесс** — `SharedContainerManager` (static Lazy).
> - Каждый тест создаёт свою БД (`CREATE DATABASE test_{guid}`), применяет миграции, сбрасывает пул и дропает БД.
> - Миграции применяются **на каждый тест** (как у обычного Testcontainers).
> - TestServer и HttpClient создаются **на каждый тест**.
> - Тесты внутри одного класса выполняются **параллельно** (изоляция per-test).
> - **Зачем нужен:** показать вклад «контейнер per-class vs per-process» в чистом виде — всё остальное идентично подходу Testcontainers.

### 3. Таблица «Сравнение по ключевым параметрам»

Добавить столбец:

| | IntegreSQL | Respawn | Testcontainers | TestcontainersShared |
|---|---|---|---|---|
| Контейнер | 1 на процесс | 1 на процесс | 1 на класс | **1 на процесс** |
| Миграции | 1 раз на процесс | 1 раз на класс | на каждый тест | **на каждый тест** |
| Сброс данных | возврат клона | DELETE по FK | EnsureDeleted | **ClearPool + DROP DATABASE** |
| TestServer (API) | новый на каждый тест | 1 на класс | новый на каждый тест | **новый на каждый тест** |
| Параллелизм внутри класса | да | нет | да | **да** |

### 4. Раздел «PowerShell скрипты»

Добавить:
```powershell
.\run-testcontainers-shared.ps1
.\run-testcontainers-shared.ps1 -Threads 8
```

### 5. Раздел «Архитектура»

Добавить абзац после Tests.Testcontainers:

> - **Tests.TestcontainersShared** (`tests/FastIntegrationTests.Tests.TestcontainersShared/`) — интеграционные тесты с **общим** контейнером PostgreSQL на процесс (~195 тестов). Инфраструктура: `SharedServiceTestBase`, `SharedApiTestBase`, `SharedContainerManager` (static Lazy), `SharedDbHandle`. Те же 7 папок и 14 классов с суффиксом `Shared`: `<Entity>ServiceSharedTests`, `<Entity>sApiSharedTests`.
>
> **Не интегрирован в BenchmarkRunner** — добавлен как отдельный подход для будущего сравнения. Запускается через `run-testcontainers-shared.ps1` или `dotnet test`. Чтобы включить в бенчмарк, нужно расширить три места: `tools/BenchmarkRunner/Program.cs:approaches`, `tools/BenchmarkRunner/Runner/TestRunner.cs:Build()`, `tools/BenchmarkRunner/Scale/ClassScaleManager.cs:_testProjectPaths`.

### 6. Раздел «Идеи для развития бенчмарка»

Добавить новый подпункт:

> ### Включить TestcontainersShared в BenchmarkRunner
> Сейчас новый подход (`Tests.TestcontainersShared`) живёт отдельно от бенчмарка. Чтобы он появился в HTML-отчёте 4-й линией, нужно: добавить `"TestcontainersShared"` в массив `approaches` в `Program.cs`, путь к проекту в `_testProjectPaths` (`ClassScaleManager.cs`) и в `Build()` (`TestRunner.cs`); проверить `BaseTestCount`, если число тестов изменится.

---

## Затронутые файлы

### Создаются

- `tests/FastIntegrationTests.Tests.TestcontainersShared/FastIntegrationTests.Tests.TestcontainersShared.csproj`
- `tests/FastIntegrationTests.Tests.TestcontainersShared/GlobalUsings.cs`
- `tests/FastIntegrationTests.Tests.TestcontainersShared/xunit.runner.json`
- `tests/FastIntegrationTests.Tests.TestcontainersShared/Infrastructure/SharedContainerManager.cs`
- `tests/FastIntegrationTests.Tests.TestcontainersShared/Infrastructure/SharedDbHandle.cs`
- `tests/FastIntegrationTests.Tests.TestcontainersShared/Infrastructure/Base/SharedServiceTestBase.cs`
- `tests/FastIntegrationTests.Tests.TestcontainersShared/Infrastructure/Base/SharedApiTestBase.cs`
- 14 файлов тестов в `Categories/`, `Customers/`, `Discounts/`, `Orders/`, `Products/`, `Reviews/`, `Suppliers/` (по 2 файла на папку)
- `run-testcontainers-shared.ps1` (корень репозитория)

### Изменяются

- `FastIntegrationTests.slnx` — добавить новый Project
- `CLAUDE.md` — 6 точечных правок (раздел команд, описание подхода, таблица, PowerShell, архитектура, идеи)

### Не изменяются

- Любые файлы в `tests/FastIntegrationTests.Tests.Testcontainers/`
- Любые файлы в `tests/FastIntegrationTests.Tests.Respawn/`
- Любые файлы в `tests/FastIntegrationTests.Tests.IntegreSQL/`
- `tools/BenchmarkRunner/**/*` — не трогаем (отложено на будущее)

---

## Проверка

После реализации:

1. **Build:** `dotnet build tests/FastIntegrationTests.Tests.TestcontainersShared` — 0 errors, 0 warnings.
2. **List tests:** `dotnet test tests/FastIntegrationTests.Tests.TestcontainersShared --list-tests` — должно показать 195 тестов с правильными именами (`<Entity>ServiceSharedTests`, `<Entity>sApiSharedTests`).
3. **Smoke-test (вручную):** `.\run-testcontainers-shared.ps1 -Threads 4`. В логе:
   - `##BENCH[container]=` ровно один раз;
   - `##BENCH[migration]=` 195 раз;
   - `##BENCH[reset]=` 195 раз;
   - В `docker ps` во время прогона — ровно один `postgres` и один `ryuk`.
   - Все 195 тестов зелёные.

Полный прогон `dotnet test` я не запускаю — это делает пользователь. Я ограничиваюсь `build` и `--list-tests`.
