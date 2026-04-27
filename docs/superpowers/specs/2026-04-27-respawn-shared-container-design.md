# Respawn: shared container + исправление ##BENCH маркеров

## Цель

Перевести Respawn с «1 контейнер на тест-класс» на «1 контейнер на весь процесс + CREATE/DROP БД на класс».
Заодно починить `##BENCH` маркеры во всех трёх подходах — сейчас они не попадают в бенчмарк из-за того, что xUnit перехватывает `Console.Out`.

---

## Часть 1: Respawn — shared container

### Проблема

`RespawnFixture.InitializeAsync()` создаёт Docker-сеть + PostgreSQL-контейнер на каждый тест-класс.
При 14 параллельных классах это 14 сетей + 14 контейнеров одновременно.
Следствие — обходной воркараунд с изолированными сетями из-за IP-коллизий (`address already in use`).

### Решение

Один контейнер на весь процесс через `static Lazy<Task<PostgreSqlContainer>>` (паттерн `IntegresSqlContainerManager`).
Каждый `RespawnFixture` создаёт уникальную БД на этом контейнере и дропает при Dispose.

### Новый файл: `RespawnContainerManager`

`tests/FastIntegrationTests.Tests.Respawn/Infrastructure/RespawnContainerManager.cs`

```csharp
public static class RespawnContainerManager
{
    private static readonly Lazy<Task<PostgreSqlContainer>> _container =
        new(() => StartAsync(), LazyThreadSafetyMode.ExecutionAndPublication);

    public static Task<PostgreSqlContainer> GetContainerAsync() => _container.Value;

    private static async Task<PostgreSqlContainer> StartAsync()
    {
        var network = new NetworkBuilder().Build();
        await network.CreateAsync();

        var container = new PostgreSqlBuilder()
            .WithNetwork(network)
            .WithImage("postgres:16-alpine")
            .WithCommand(
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

`max_connections=500` — 14 параллельных классов, у каждого своя Npgsql connection pool.
Контейнер не останавливается явно — Ryuk убирает его после завершения процесса.

### Изменённый `RespawnFixture`

`tests/FastIntegrationTests.Tests.Respawn/Infrastructure/Fixtures/RespawnFixture.cs`

Что убирается:
- поле `_network`, поле `_container`
- весь блок `NetworkBuilder` + `PostgreSqlBuilder` в `InitializeAsync`

Что добавляется:
- поле `_dbName` (уникальное имя БД)
- поле `_adminConnectionString` (для CREATE/DROP DATABASE)
- в `InitializeAsync`:
  1. `var container = await RespawnContainerManager.GetContainerAsync()`
  2. `_adminConnectionString = container.GetConnectionString()` (подключение к БД `postgres`)
  3. `_dbName = $"respawn_{Guid.NewGuid():N}"`
  4. Выполнить `CREATE DATABASE "{_dbName}"` через admin-соединение
  5. Построить `ConnectionString` через `NpgsqlConnectionStringBuilder` с `Database = _dbName`
  6. `MigrateAsync` + `Respawner.CreateAsync` — как раньше
  7. `##BENCH[migration]` замеряет шаги 4–6 целиком (CREATE DATABASE + MigrateAsync)

- в `DisposeAsync`:
  1. Завершить все соединения с БД и дропнуть её через admin-соединение:
     ```csharp
     await using var conn = new NpgsqlConnection(_adminConnectionString);
     await conn.OpenAsync();
     await using var cmd = conn.CreateCommand();
     cmd.CommandText = $"""
         SELECT pg_terminate_backend(pid)
         FROM pg_stat_activity
         WHERE datname = '{_dbName}' AND pid <> pg_backend_pid();
         DROP DATABASE IF EXISTS "{_dbName}";
         """;
     await cmd.ExecuteNonQueryAsync();
     ```
  2. Убрать `await _container.DisposeAsync()` и `await _network.DisposeAsync()`

`ResetAsync`, `RespawnApiFixture`, оба базовых класса, все 14 тест-классов — **без изменений**.

---

## Часть 2: исправление `##BENCH` маркеров

### Проблема

xUnit вызывает `Console.SetOut()` и перехватывает весь вывод в внутренний буфер.
`TestRunner` читает stdout через `OutputDataReceived` — туда маркеры не попадают.
Итог: `migrationSeconds=0` и `resetSeconds=0` у всех трёх подходов в stacked bar.

Дополнительно: у Testcontainers отсутствует `##BENCH[reset]` — `EnsureDeletedAsync` не замеряется.

### Решение

`ConcurrentQueue<string>` + flush в файл через `AppDomain.CurrentDomain.ProcessExit`.
`TestRunner` передаёт путь через `BENCH_LOG_FILE` env var, читает файл после завершения.

### Новый файл: `BenchmarkLogger`

`tests/FastIntegrationTests.Tests.Shared/Infrastructure/BenchmarkLogger.cs`

```csharp
namespace FastIntegrationTests.Tests.Infrastructure;

/// <summary>
/// Записывает ##BENCH маркеры в файл без блокировок в горячем пути.
/// Console.WriteLine не работает — xUnit перехватывает Console.Out.
/// Путь к файлу задаётся через BENCH_LOG_FILE env var (TestRunner).
/// При отсутствии env var — no-op.
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

### GlobalUsings — три тест-проекта

Добавить в каждый `GlobalUsings.cs`:
```csharp
global using FastIntegrationTests.Tests.Infrastructure;
```

Файлы:
- `tests/FastIntegrationTests.Tests.IntegreSQL/GlobalUsings.cs`
- `tests/FastIntegrationTests.Tests.Respawn/GlobalUsings.cs`
- `tests/FastIntegrationTests.Tests.Testcontainers/GlobalUsings.cs`

### Замены `Console.WriteLine` → `BenchmarkLogger.Write`

| Файл | Маркер |
|------|--------|
| `tests/.../IntegreSQL/IntegresSqlContainerManager.cs:115` | `migration` |
| `tests/.../IntegreSQL/Base/AppServiceTestBase.cs:41` | `reset` |
| `tests/.../IntegreSQL/Base/ComponentTestBase.cs:43` | `reset` |
| `tests/.../Testcontainers/Factories/TestDbFactory.cs:40` | `migration` |

Вместо:
```csharp
Console.WriteLine($"##BENCH[migration]={sw.ElapsedMilliseconds}");
```
Писать:
```csharp
BenchmarkLogger.Write("migration", sw.ElapsedMilliseconds);
```

### Добавить недостающий `##BENCH[reset]` в Testcontainers

`tests/FastIntegrationTests.Tests.Testcontainers/Infrastructure/Base/ServiceTestBase.cs` — `DisposeAsync`:
```csharp
var sw = Stopwatch.StartNew();
await _context.Database.EnsureDeletedAsync();
sw.Stop();
BenchmarkLogger.Write("reset", sw.ElapsedMilliseconds);
```

`tests/FastIntegrationTests.Tests.Testcontainers/Infrastructure/Base/ApiTestBase.cs` — `DisposeAsync`:
```csharp
var sw = Stopwatch.StartNew();
await _context.Database.EnsureDeletedAsync();
sw.Stop();
BenchmarkLogger.Write("reset", sw.ElapsedMilliseconds);
```

### Изменения в `TestRunner`

`tools/BenchmarkRunner/Runner/TestRunner.cs`

`psi` создаётся внутри `RunCapture` — добавляем необязательный параметр `env`:

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
    // ... остаток без изменений
}
```

В `RunTest` генерируем путь к лог-файлу и передаём через `env`:

```csharp
private (double Elapsed, bool Success, string Output, double MigrationSeconds, double ResetSeconds) RunTest(BenchmarkScenario scenario)
{
    var benchLogFile = Path.Combine(
        Path.GetTempPath(), $"bench-{Guid.NewGuid():N}.log");

    var args = $"test tests/FastIntegrationTests.Tests.{scenario.Approach}"
             + $" --no-build"
             + $" -- xUnit.MaxParallelThreads={scenario.MaxParallelThreads}";

    var sw = Stopwatch.StartNew();
    var (output, code) = RunCapture("dotnet", args,
        new Dictionary<string, string> { ["BENCH_LOG_FILE"] = benchLogFile });
    sw.Stop();

    WaitForRyukToStop();

    var benchLines = File.Exists(benchLogFile)
        ? File.ReadAllLines(benchLogFile)
        : Array.Empty<string>();
    if (File.Exists(benchLogFile))
        File.Delete(benchLogFile);

    var (migrationMs, resetMs) = ParseBenchLines(string.Join("\n", benchLines));
    return (sw.Elapsed.TotalSeconds, code == 0, output, migrationMs / 1000.0, resetMs / 1000.0);
}
```

`ParseBenchLines` — без изменений.

---

## Затронутые файлы

### Создаются
- `tests/FastIntegrationTests.Tests.Respawn/Infrastructure/RespawnContainerManager.cs`
- `tests/FastIntegrationTests.Tests.Shared/Infrastructure/BenchmarkLogger.cs`

### Изменяются
- `tests/FastIntegrationTests.Tests.Respawn/Infrastructure/Fixtures/RespawnFixture.cs`
- `tests/FastIntegrationTests.Tests.IntegreSQL/GlobalUsings.cs`
- `tests/FastIntegrationTests.Tests.Respawn/GlobalUsings.cs`
- `tests/FastIntegrationTests.Tests.Testcontainers/GlobalUsings.cs`
- `tests/FastIntegrationTests.Tests.IntegreSQL/Infrastructure/IntegreSQL/IntegresSqlContainerManager.cs`
- `tests/FastIntegrationTests.Tests.IntegreSQL/Infrastructure/Base/AppServiceTestBase.cs`
- `tests/FastIntegrationTests.Tests.IntegreSQL/Infrastructure/Base/ComponentTestBase.cs`
- `tests/FastIntegrationTests.Tests.Testcontainers/Infrastructure/Factories/TestDbFactory.cs`
- `tests/FastIntegrationTests.Tests.Testcontainers/Infrastructure/Base/ServiceTestBase.cs`
- `tests/FastIntegrationTests.Tests.Testcontainers/Infrastructure/Base/ApiTestBase.cs`
- `tools/BenchmarkRunner/Runner/TestRunner.cs`

### Не изменяются
- `RespawnApiFixture.cs`
- `RespawnServiceTestBase.cs`, `RespawnApiTestBase.cs`
- Все 14 тест-классов Respawn
- `ContainerFixture.cs` (Testcontainers, отдельная задача)
- CLAUDE.md, README.md (описание "DELETE по FK-порядку" уже актуально)
