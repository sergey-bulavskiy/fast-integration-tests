# Respawn shared container + ##BENCH fix: Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Перевести Respawn на один shared PostgreSQL-контейнер на весь процесс и починить ##BENCH маркеры во всех трёх подходах (сейчас они уходят в буфер xUnit, а не в stdout).

**Architecture:** Два независимых изменения. (1) Новый `RespawnContainerManager` (static Lazy по образцу `IntegresSqlContainerManager`) запускает один контейнер на весь процесс; `RespawnFixture` создаёт уникальную БД на нём и дропает при Dispose — вместо создания нового контейнера на каждый класс. (2) `BenchmarkLogger` в Tests.Shared пишет ##BENCH строки в `ConcurrentQueue` (lock-free) и сбрасывает в файл через `AppDomain.ProcessExit`; `TestRunner` передаёт путь через `BENCH_LOG_FILE` env var и читает файл после завершения `dotnet test`.

**Tech Stack:** .NET 8, xUnit v2, Testcontainers.PostgreSql, Respawn 7.0, Npgsql, System.Collections.Concurrent

**Spec:** `docs/superpowers/specs/2026-04-27-respawn-shared-container-design.md`

---

## Структура файлов

| Действие | Файл |
|----------|------|
| Создать | `tests/FastIntegrationTests.Tests.Shared/Infrastructure/BenchmarkLogger.cs` |
| Создать | `tests/FastIntegrationTests.Tests.Respawn/Infrastructure/RespawnContainerManager.cs` |
| Изменить | `tests/FastIntegrationTests.Tests.Respawn/Infrastructure/Fixtures/RespawnFixture.cs` |
| Изменить | `tests/FastIntegrationTests.Tests.IntegreSQL/GlobalUsings.cs` |
| Изменить | `tests/FastIntegrationTests.Tests.Respawn/GlobalUsings.cs` |
| Изменить | `tests/FastIntegrationTests.Tests.Testcontainers/GlobalUsings.cs` |
| Изменить | `tests/FastIntegrationTests.Tests.IntegreSQL/Infrastructure/IntegreSQL/IntegresSqlContainerManager.cs` |
| Изменить | `tests/FastIntegrationTests.Tests.IntegreSQL/Infrastructure/Base/AppServiceTestBase.cs` |
| Изменить | `tests/FastIntegrationTests.Tests.IntegreSQL/Infrastructure/Base/ComponentTestBase.cs` |
| Изменить | `tests/FastIntegrationTests.Tests.Testcontainers/Infrastructure/Factories/TestDbFactory.cs` |
| Изменить | `tests/FastIntegrationTests.Tests.Testcontainers/Infrastructure/Base/ServiceTestBase.cs` |
| Изменить | `tests/FastIntegrationTests.Tests.Testcontainers/Infrastructure/Base/ApiTestBase.cs` |
| Изменить | `tools/BenchmarkRunner/Runner/TestRunner.cs` |
| Изменить | `CLAUDE.md` |
| Изменить | `README.md` |

---

## Task 1: BenchmarkLogger

**Files:**
- Create: `tests/FastIntegrationTests.Tests.Shared/Infrastructure/BenchmarkLogger.cs`

- [ ] **Создать файл** `tests/FastIntegrationTests.Tests.Shared/Infrastructure/BenchmarkLogger.cs`:

```csharp
using System.Collections.Concurrent;

namespace FastIntegrationTests.Tests.Infrastructure;

/// <summary>
/// Записывает ##BENCH маркеры в файл без блокировок в горячем пути.
/// Console.WriteLine не работает — xUnit перехватывает Console.Out через Console.SetOut().
/// Путь к файлу передаётся через BENCH_LOG_FILE env var (устанавливается BenchmarkRunner).
/// При отсутствии env var — no-op: работает вне бенчмарка без побочных эффектов.
/// </summary>
public static class BenchmarkLogger
{
    private static readonly ConcurrentQueue<string> _lines = new();
    private static readonly string? _path =
        Environment.GetEnvironmentVariable("BENCH_LOG_FILE");

    static BenchmarkLogger()
    {
        if (_path is not null)
            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
                File.WriteAllLines(_path, _lines);
    }

    /// <summary>Добавляет строку ##BENCH[key]=ms в очередь (lock-free).</summary>
    /// <param name="key">Ключ маркера: <c>migration</c> или <c>reset</c>.</param>
    /// <param name="ms">Время в миллисекундах.</param>
    public static void Write(string key, long ms)
    {
        if (_path is null) return;
        _lines.Enqueue($"##BENCH[{key}]={ms}");
    }
}
```

- [ ] **Проверить сборку:**

```bash
dotnet build tests/FastIntegrationTests.Tests.Shared --nologo -v minimal
```

Ожидается: `Build succeeded.`

- [ ] **Коммит:**

```bash
git add tests/FastIntegrationTests.Tests.Shared/Infrastructure/BenchmarkLogger.cs
git commit -m "feat: добавить BenchmarkLogger — ConcurrentQueue + ProcessExit вместо Console.WriteLine"
```

---

## Task 2: GlobalUsings — добавить неймспейс BenchmarkLogger

**Files:**
- Modify: `tests/FastIntegrationTests.Tests.IntegreSQL/GlobalUsings.cs`
- Modify: `tests/FastIntegrationTests.Tests.Respawn/GlobalUsings.cs`
- Modify: `tests/FastIntegrationTests.Tests.Testcontainers/GlobalUsings.cs`

- [ ] **Добавить** строку в конец каждого из трёх файлов `GlobalUsings.cs`:

```csharp
global using FastIntegrationTests.Tests.Infrastructure;
```

Файлы после изменения (все три одинаковы по этой строке):
- `tests/FastIntegrationTests.Tests.IntegreSQL/GlobalUsings.cs`
- `tests/FastIntegrationTests.Tests.Respawn/GlobalUsings.cs`
- `tests/FastIntegrationTests.Tests.Testcontainers/GlobalUsings.cs`

- [ ] **Проверить сборку всего решения:**

```bash
dotnet build --nologo -v minimal
```

Ожидается: `Build succeeded.`

- [ ] **Коммит:**

```bash
git add tests/FastIntegrationTests.Tests.IntegreSQL/GlobalUsings.cs
git add tests/FastIntegrationTests.Tests.Respawn/GlobalUsings.cs
git add tests/FastIntegrationTests.Tests.Testcontainers/GlobalUsings.cs
git commit -m "feat: добавить global using FastIntegrationTests.Tests.Infrastructure в три тест-проекта"
```

---

## Task 3: IntegreSQL — заменить Console.WriteLine на BenchmarkLogger.Write

**Files:**
- Modify: `tests/FastIntegrationTests.Tests.IntegreSQL/Infrastructure/IntegreSQL/IntegresSqlContainerManager.cs:115`
- Modify: `tests/FastIntegrationTests.Tests.IntegreSQL/Infrastructure/Base/AppServiceTestBase.cs:41`
- Modify: `tests/FastIntegrationTests.Tests.IntegreSQL/Infrastructure/Base/ComponentTestBase.cs:43`

- [ ] **В `IntegresSqlContainerManager.cs`** заменить строку 115:

```csharp
// Было:
Console.WriteLine($"##BENCH[migration]={migSw.ElapsedMilliseconds}");

// Стало:
BenchmarkLogger.Write("migration", migSw.ElapsedMilliseconds);
```

- [ ] **В `AppServiceTestBase.cs`** заменить строку 41:

```csharp
// Было:
Console.WriteLine($"##BENCH[reset]={sw.ElapsedMilliseconds}");

// Стало:
BenchmarkLogger.Write("reset", sw.ElapsedMilliseconds);
```

- [ ] **В `ComponentTestBase.cs`** заменить строку 43:

```csharp
// Было:
Console.WriteLine($"##BENCH[reset]={sw.ElapsedMilliseconds}");

// Стало:
BenchmarkLogger.Write("reset", sw.ElapsedMilliseconds);
```

- [ ] **Проверить сборку и тест-дискавери:**

```bash
dotnet build tests/FastIntegrationTests.Tests.IntegreSQL --nologo -v minimal
dotnet test tests/FastIntegrationTests.Tests.IntegreSQL --list-tests 2>/dev/null | tail -5
```

Ожидается: сборка без ошибок, список тестов не изменился.

- [ ] **Коммит:**

```bash
git add tests/FastIntegrationTests.Tests.IntegreSQL/Infrastructure/IntegreSQL/IntegresSqlContainerManager.cs
git add tests/FastIntegrationTests.Tests.IntegreSQL/Infrastructure/Base/AppServiceTestBase.cs
git add tests/FastIntegrationTests.Tests.IntegreSQL/Infrastructure/Base/ComponentTestBase.cs
git commit -m "fix: заменить Console.WriteLine на BenchmarkLogger.Write в IntegreSQL"
```

---

## Task 4: Testcontainers — заменить Console.WriteLine + добавить недостающий reset маркер

**Files:**
- Modify: `tests/FastIntegrationTests.Tests.Testcontainers/Infrastructure/Factories/TestDbFactory.cs:40`
- Modify: `tests/FastIntegrationTests.Tests.Testcontainers/Infrastructure/Base/ServiceTestBase.cs`
- Modify: `tests/FastIntegrationTests.Tests.Testcontainers/Infrastructure/Base/ApiTestBase.cs`

- [ ] **В `TestDbFactory.cs`** заменить строку 40:

```csharp
// Было:
Console.WriteLine($"##BENCH[migration]={sw.ElapsedMilliseconds}");

// Стало:
BenchmarkLogger.Write("migration", sw.ElapsedMilliseconds);
```

- [ ] **В `ServiceTestBase.cs`** обернуть `EnsureDeletedAsync` в `DisposeAsync` маркером:

```csharp
// Было:
public async Task DisposeAsync()
{
    if (_context is null) return;
    await _context.Database.EnsureDeletedAsync();
    await _context.DisposeAsync();
}

// Стало:
public async Task DisposeAsync()
{
    if (_context is null) return;
    var sw = System.Diagnostics.Stopwatch.StartNew();
    await _context.Database.EnsureDeletedAsync();
    sw.Stop();
    BenchmarkLogger.Write("reset", sw.ElapsedMilliseconds);
    await _context.DisposeAsync();
}
```

- [ ] **В `ApiTestBase.cs`** обернуть `EnsureDeletedAsync` в `DisposeAsync` маркером:

```csharp
// Было:
public async Task DisposeAsync()
{
    if (_context is null) return;
    Client?.Dispose();
    if (_factory is not null) await _factory.DisposeAsync();
    await _context.Database.EnsureDeletedAsync();
    await _context.DisposeAsync();
}

// Стало:
public async Task DisposeAsync()
{
    if (_context is null) return;
    Client?.Dispose();
    if (_factory is not null) await _factory.DisposeAsync();
    var sw = System.Diagnostics.Stopwatch.StartNew();
    await _context.Database.EnsureDeletedAsync();
    sw.Stop();
    BenchmarkLogger.Write("reset", sw.ElapsedMilliseconds);
    await _context.DisposeAsync();
}
```

- [ ] **Проверить сборку и тест-дискавери:**

```bash
dotnet build tests/FastIntegrationTests.Tests.Testcontainers --nologo -v minimal
dotnet test tests/FastIntegrationTests.Tests.Testcontainers --list-tests 2>/dev/null | tail -5
```

Ожидается: сборка без ошибок, список тестов не изменился.

- [ ] **Коммит:**

```bash
git add tests/FastIntegrationTests.Tests.Testcontainers/Infrastructure/Factories/TestDbFactory.cs
git add tests/FastIntegrationTests.Tests.Testcontainers/Infrastructure/Base/ServiceTestBase.cs
git add tests/FastIntegrationTests.Tests.Testcontainers/Infrastructure/Base/ApiTestBase.cs
git commit -m "fix: заменить Console.WriteLine на BenchmarkLogger.Write в Testcontainers + добавить ##BENCH[reset]"
```

---

## Task 5: TestRunner — поддержка BENCH_LOG_FILE

**Files:**
- Modify: `tools/BenchmarkRunner/Runner/TestRunner.cs`

Текущая логика: `RunCapture` создаёт `ProcessStartInfo` внутри себя и возвращает `(output, code)`. Маркеры парсятся из stdout. После изменения: `RunCapture` принимает необязательный словарь env vars; `RunTest` генерирует путь к лог-файлу, передаёт через env var, читает файл после завершения.

- [ ] **Заменить сигнатуру и тело `RunCapture`** — добавить параметр `env`:

```csharp
private (string Output, int Code) RunCapture(
    string filename, string args,
    IReadOnlyDictionary<string, string>? env = null)
{
    var psi = new ProcessStartInfo(filename, args)
    {
        WorkingDirectory       = _repoRoot,
        UseShellExecute        = false,
        RedirectStandardOutput = true,
        RedirectStandardError  = true,
    };
    if (env is not null)
        foreach (var (k, v) in env)
            psi.Environment[k] = v;

    var lines = new ConcurrentQueue<string>();
    using var process = Process.Start(psi)!;
    process.OutputDataReceived += (_, ev) => { if (ev.Data is not null) lines.Enqueue(ev.Data); };
    process.ErrorDataReceived  += (_, ev) => { if (ev.Data is not null) lines.Enqueue(ev.Data); };
    process.BeginOutputReadLine();
    process.BeginErrorReadLine();

    var completed = process.WaitForExit(_timeout);
    if (!completed)
    {
        process.Kill(entireProcessTree: true);
        return ($"TIMEOUT: process exceeded {_timeout.TotalMinutes:F0} minutes", -1);
    }

    return (string.Join(Environment.NewLine, lines), process.ExitCode);
}
```

- [ ] **Заменить тело `RunTest`** — генерировать benchLogFile, читать файл после теста:

```csharp
private (double Elapsed, bool Success, string Output, double MigrationSeconds, double ResetSeconds) RunTest(BenchmarkScenario scenario)
{
    var benchLogFile = Path.Combine(
        Path.GetTempPath(), $"bench-{Guid.NewGuid():N}.log");

    var args =
        $"test tests/FastIntegrationTests.Tests.{scenario.Approach}" +
        $" --no-build" +
        $" -- xUnit.MaxParallelThreads={scenario.MaxParallelThreads}";

    var sw = Stopwatch.StartNew();
    var (output, code) = RunCapture("dotnet", args,
        new Dictionary<string, string> { ["BENCH_LOG_FILE"] = benchLogFile });
    sw.Stop();

    WaitForRyukToStop();

    var benchContent = File.Exists(benchLogFile)
        ? File.ReadAllText(benchLogFile)
        : string.Empty;
    if (File.Exists(benchLogFile))
        File.Delete(benchLogFile);

    var (migrationMs, resetMs) = ParseBenchLines(benchContent);
    return (sw.Elapsed.TotalSeconds, code == 0, output, migrationMs / 1000.0, resetMs / 1000.0);
}
```

`ParseBenchLines` — без изменений, она уже работает со строками.

- [ ] **Проверить сборку:**

```bash
dotnet build tools/BenchmarkRunner --nologo -v minimal
```

Ожидается: `Build succeeded.`

- [ ] **Коммит:**

```bash
git add tools/BenchmarkRunner/Runner/TestRunner.cs
git commit -m "fix: TestRunner читает ##BENCH маркеры из файла через BENCH_LOG_FILE env var"
```

---

## Task 6: RespawnContainerManager

**Files:**
- Create: `tests/FastIntegrationTests.Tests.Respawn/Infrastructure/RespawnContainerManager.cs`

- [ ] **Создать файл** `tests/FastIntegrationTests.Tests.Respawn/Infrastructure/RespawnContainerManager.cs`:

```csharp
using DotNet.Testcontainers.Builders;
using Testcontainers.PostgreSql;

namespace FastIntegrationTests.Tests.Infrastructure;

/// <summary>
/// Запускает один PostgreSQL-контейнер в Docker один раз на весь процесс.
/// Все тест-классы Respawn разделяют этот контейнер; каждый создаёт свою БД.
/// </summary>
/// <remarks>
/// Контейнер не останавливается явно — Ryuk-агент Testcontainers убирает его после завершения процесса.
/// </remarks>
public static class RespawnContainerManager
{
    private static readonly Lazy<Task<PostgreSqlContainer>> _container =
        new(() => StartAsync(), LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// Возвращает запущенный контейнер.
    /// При первом вызове стартует контейнер; последующие возвращают кешированный результат.
    /// </summary>
    public static Task<PostgreSqlContainer> GetContainerAsync() => _container.Value;

    private static async Task<PostgreSqlContainer> StartAsync()
    {
        var network = new NetworkBuilder().Build();
        await network.CreateAsync();

        // Параметры производительности PostgreSQL для тестовой среды.
        // Рекомендованы авторами IntegreSQL:
        // https://github.com/allaboutapps/integresql/blob/master/README.md
        // ⚠ НИКОГДА не переносить в продакшн — при сбое питания возможна потеря данных.
        var container = new PostgreSqlBuilder()
            .WithNetwork(network)
            .WithImage("postgres:16-alpine")
            .WithCommand(
                // 14 параллельных классов × connection pool — дефолтных 100 не хватает.
                "-c", "max_connections=500",
                "-c", "fsync=off",
                "-c", "synchronous_commit=off",
                "-c", "full_page_writes=off",
                "-c", "shared_buffers=128MB"
            )
            .Build();
        await container.StartAsync();
        return container;
    }
}
```

- [ ] **Проверить сборку:**

```bash
dotnet build tests/FastIntegrationTests.Tests.Respawn --nologo -v minimal
```

Ожидается: `Build succeeded.`

- [ ] **Коммит:**

```bash
git add tests/FastIntegrationTests.Tests.Respawn/Infrastructure/RespawnContainerManager.cs
git commit -m "feat: RespawnContainerManager — один shared PostgreSQL-контейнер на весь процесс"
```

---

## Task 7: RespawnFixture — переписать на shared container

**Files:**
- Modify: `tests/FastIntegrationTests.Tests.Respawn/Infrastructure/Fixtures/RespawnFixture.cs`

Текущий файл создаёт Docker-сеть + контейнер на каждый класс. Новый — получает контейнер из `RespawnContainerManager`, создаёт уникальную БД и дропает её при Dispose.

- [ ] **Полностью заменить содержимое** `tests/FastIntegrationTests.Tests.Respawn/Infrastructure/Fixtures/RespawnFixture.cs`:

```csharp
using Npgsql;
using Respawn;

namespace FastIntegrationTests.Tests.Infrastructure.Fixtures;

/// <summary>
/// Получает общий контейнер PostgreSQL из <see cref="RespawnContainerManager"/>,
/// создаёт изолированную базу данных один раз на класс, применяет миграции EF Core
/// и сбрасывает данные через Respawn перед каждым тестом.
/// </summary>
public class RespawnFixture : IAsyncLifetime
{
    private string _dbName = null!;
    private string _adminConnectionString = null!;
    private Respawner _respawner = null!;

    /// <summary>Строка подключения к тестовой БД.</summary>
    public string ConnectionString { get; private set; } = null!;

    /// <inheritdoc />
    public virtual async Task InitializeAsync()
    {
        var container = await RespawnContainerManager.GetContainerAsync();
        _adminConnectionString = container.GetConnectionString();
        _dbName = $"respawn_{Guid.NewGuid():N}";

        await using var adminConn = new NpgsqlConnection(_adminConnectionString);
        await adminConn.OpenAsync();

        var migSw = System.Diagnostics.Stopwatch.StartNew();

        await using (var createCmd = adminConn.CreateCommand())
        {
            createCmd.CommandText = $"CREATE DATABASE \"{_dbName}\"";
            await createCmd.ExecuteNonQueryAsync();
        }

        var csb = new NpgsqlConnectionStringBuilder(_adminConnectionString)
        {
            Database = _dbName
        };
        ConnectionString = csb.ConnectionString;

        var options = new DbContextOptionsBuilder<ShopDbContext>()
            .UseNpgsql(ConnectionString).Options;
        await using var ctx = new ShopDbContext(options);
        await ctx.Database.MigrateAsync();

        migSw.Stop();
        BenchmarkLogger.Write("migration", migSw.ElapsedMilliseconds);

        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        _respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
        });
    }

    /// <summary>Сбрасывает все данные через Respawn (схема сохраняется).</summary>
    public async Task ResetAsync()
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await _respawner.ResetAsync(conn);
        sw.Stop();
        BenchmarkLogger.Write("reset", sw.ElapsedMilliseconds);
    }

    /// <inheritdoc />
    public virtual async Task DisposeAsync()
    {
        await using var adminConn = new NpgsqlConnection(_adminConnectionString);
        await adminConn.OpenAsync();
        await using var cmd = adminConn.CreateCommand();
        cmd.CommandText = $"""
            SELECT pg_terminate_backend(pid)
            FROM pg_stat_activity
            WHERE datname = '{_dbName}' AND pid <> pg_backend_pid();
            DROP DATABASE IF EXISTS "{_dbName}";
            """;
        await cmd.ExecuteNonQueryAsync();
    }
}
```

- [ ] **Проверить сборку и тест-дискавери:**

```bash
dotnet build tests/FastIntegrationTests.Tests.Respawn --nologo -v minimal
dotnet test tests/FastIntegrationTests.Tests.Respawn --list-tests 2>/dev/null | tail -5
```

Ожидается: сборка без ошибок, 165 тестов в списке (без изменений числа).

- [ ] **Коммит:**

```bash
git add tests/FastIntegrationTests.Tests.Respawn/Infrastructure/Fixtures/RespawnFixture.cs
git commit -m "refactor: RespawnFixture — shared container + CREATE/DROP DATABASE вместо контейнера на класс"
```

---

## Task 8: Обновить CLAUDE.md и README.md

**Files:**
- Modify: `CLAUDE.md`
- Modify: `README.md`

- [ ] **В `CLAUDE.md`** найти блок описания Respawn и обновить:

```markdown
// Было:
**Respawn** (`RespawnServiceTestBase` / `RespawnApiTestBase`):
- Один контейнер PostgreSQL **на класс** (через `IClassFixture<RespawnFixture>`).
- Миграции применяются **один раз на класс** в `RespawnFixture.InitializeAsync()`.
- Между тестами — Respawn выполняет DELETE в детерминированном порядке по FK-зависимостям, схема сохраняется.
- TestServer и HttpClient создаются **один раз на класс** и переиспользуются.
- Тесты внутри одного класса выполняются **последовательно** (общая БД).

// Стало:
**Respawn** (`RespawnServiceTestBase` / `RespawnApiTestBase`):
- Один контейнер PostgreSQL **на весь процесс** — `RespawnContainerManager` (static Lazy).
- Каждый класс создаёт отдельную БД (`CREATE DATABASE`) и дропает при завершении.
- Миграции применяются **один раз на класс** в `RespawnFixture.InitializeAsync()`.
- Между тестами — Respawn выполняет DELETE в детерминированном порядке по FK-зависимостям, схема сохраняется.
- TestServer и HttpClient создаются **один раз на класс** и переиспользуются.
- Тесты внутри одного класса выполняются **последовательно** (общая БД).
```

- [ ] **В `CLAUDE.md`** в таблице сравнения найти строку `Контейнер` и обновить:

```markdown
// Было:
| Контейнер | 1 на процесс | 1 на класс | 1 на класс |

// Стало:
| Контейнер | 1 на процесс | 1 на процесс | 1 на класс |
```

- [ ] **В `README.md`** в таблице сравнения найти строку `Контейнер` и обновить:

```markdown
// Было:
| Контейнер | 1 на процесс | 1 на класс | 1 на класс |

// Стало:
| Контейнер | 1 на процесс | 1 на процесс | 1 на класс |
```

- [ ] **Коммит:**

```bash
git add CLAUDE.md README.md
git commit -m "docs: обновить описание Respawn — shared container вместо контейнера на класс"
```

---

## Self-review: покрытие спека

| Требование спека | Задача |
|-----------------|--------|
| BenchmarkLogger: ConcurrentQueue + ProcessExit | Task 1 |
| GlobalUsings: добавить неймспейс | Task 2 |
| IntegreSQL: замена Console.WriteLine (3 места) | Task 3 |
| Testcontainers: замена Console.WriteLine + reset маркеры (3 места) | Task 4 |
| TestRunner: BENCH_LOG_FILE + чтение файла | Task 5 |
| RespawnContainerManager: static Lazy, max_connections=500 | Task 6 |
| RespawnFixture: CREATE/DROP DB, BenchmarkLogger, без сети/контейнера | Task 7 |
| Docs: CLAUDE.md и README.md | Task 8 |
