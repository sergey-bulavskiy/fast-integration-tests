# Migration Timing Decomposition — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Добавить в HTML-отчёт бенчмарка stacked bar chart, показывающий долю миграций / сброса данных / бизнес-логики для каждого подхода при минимальном (17) и максимальном (117) числе миграций.

**Architecture:** Каждый тест-компонент выводит `##BENCH[migration]=Nms` и `##BENCH[reset]=Nms` в stdout сразу после операции. BenchmarkRunner суммирует все строки с одним ключом и сохраняет в новые поля `BenchmarkResult`. `report-template.html` строит 4-й график из данных Сценария 1.

**Tech Stack:** .NET 8, xUnit, Chart.js v4, C# 12 records.

---

### Task 1: Расширить модель BenchmarkResult

**Files:**
- Modify: `tools/BenchmarkRunner/Models/BenchmarkResult.cs`

- [ ] **Шаг 1: Заменить содержимое файла**

```csharp
// tools/BenchmarkRunner/Models/BenchmarkResult.cs
namespace BenchmarkRunner.Models;

/// <summary>
/// Результат одного бенчмарка.
/// </summary>
/// <param name="Scenario">Сценарий бенчмарка</param>
/// <param name="ElapsedSeconds">Время выполнения в секундах</param>
/// <param name="MigrationSeconds">Суммарное время миграций в секундах</param>
/// <param name="ResetSeconds">Суммарное время сброса данных в секундах</param>
/// <param name="Success">Успешное ли выполнение</param>
public record BenchmarkResult(
    BenchmarkScenario Scenario,
    double ElapsedSeconds,
    double MigrationSeconds,
    double ResetSeconds,
    bool Success
);
```

- [ ] **Шаг 2: Собрать проект, убедиться в ошибках компиляции**

```bash
dotnet build tools/BenchmarkRunner --nologo -v minimal
```

Ожидается: ошибки `CS7036` — `Warmup` и `Run` в `TestRunner.cs` создают `BenchmarkResult` со старой сигнатурой. Это нормально — фиксим в следующей задаче.

- [ ] **Шаг 3: Закоммитить**

```bash
git add tools/BenchmarkRunner/Models/BenchmarkResult.cs
git commit -m "feat: добавить MigrationSeconds и ResetSeconds в BenchmarkResult"
```

---

### Task 2: Парсить ##BENCH-строки в TestRunner

**Files:**
- Modify: `tools/BenchmarkRunner/Runner/TestRunner.cs`

- [ ] **Шаг 1: Добавить приватный метод парсинга**

Вставить после строки 89 (после `return (sw.Elapsed.TotalSeconds, code == 0, output);`), изменив `RunTest` и добавив `ParseBenchLines`:

```csharp
private (double Elapsed, bool Success, string Output, double MigrationSeconds, double ResetSeconds) RunTest(BenchmarkScenario scenario)
{
    var args =
        $"test tests/FastIntegrationTests.Tests.{scenario.Approach}" +
        $" --no-build" +
        $" -- xUnit.MaxParallelThreads={scenario.MaxParallelThreads}";

    var sw = Stopwatch.StartNew();
    var (output, code) = RunCapture("dotnet", args, ("TEST_REPEAT", scenario.TestRepeat.ToString()));
    sw.Stop();

    var (migrationMs, resetMs) = ParseBenchLines(output);
    return (sw.Elapsed.TotalSeconds, code == 0, output, migrationMs / 1000.0, resetMs / 1000.0);
}

private static (long MigrationMs, long ResetMs) ParseBenchLines(string output)
{
    long migrationMs = 0, resetMs = 0;
    foreach (var line in output.Split('\n'))
    {
        var t = line.TrimEnd('\r');
        if (t.StartsWith("##BENCH[migration]=") &&
            long.TryParse(t["##BENCH[migration]=".Length..], out var m))
            migrationMs += m;
        else if (t.StartsWith("##BENCH[reset]=") &&
            long.TryParse(t["##BENCH[reset]=".Length..], out var r))
            resetMs += r;
    }
    return (migrationMs, resetMs);
}
```

- [ ] **Шаг 2: Обновить Warmup и Run**

Заменить строку 61:
```csharp
// было:
return new BenchmarkResult(scenario, elapsed, success);
// стало:
return new BenchmarkResult(scenario, elapsed, migrationSeconds, resetSeconds, success);
```

В `Warmup` (строки 57–62), полная замена:
```csharp
public BenchmarkResult Warmup(BenchmarkScenario scenario)
{
    Console.Write(FormatPrefix("[WRM]", scenario));
    var (elapsed, success, output, migrationSeconds, resetSeconds) = RunTest(scenario);
    Console.WriteLine(FormatSuffix(elapsed, success));
    if (!success)
        LogFailure(scenario, output);
    return new BenchmarkResult(scenario, elapsed, migrationSeconds, resetSeconds, success);
}
```

В `Run` (строки 65–75):
```csharp
public BenchmarkResult Run(BenchmarkScenario scenario)
{
    _currentRun++;
    var tag = _totalRuns > 0 ? $"[{_currentRun,2}/{_totalRuns}]" : "[   ]";
    Console.Write(FormatPrefix(tag, scenario));
    var (elapsed, success, output, migrationSeconds, resetSeconds) = RunTest(scenario);
    Console.WriteLine(FormatSuffix(elapsed, success));
    if (!success)
        LogFailure(scenario, output);
    return new BenchmarkResult(scenario, elapsed, migrationSeconds, resetSeconds, success);
}
```

- [ ] **Шаг 3: Собрать и проверить чистую компиляцию**

```bash
dotnet build tools/BenchmarkRunner --nologo -v minimal
```

Ожидается: `Build succeeded. 0 Warning(s). 0 Error(s).`

- [ ] **Шаг 4: Закоммитить**

```bash
git add tools/BenchmarkRunner/Runner/TestRunner.cs
git commit -m "feat: парсить ##BENCH-строки из stdout в BenchmarkRunner"
```

---

### Task 3: Инструментировать IntegreSQL

**Files:**
- Modify: `tests/FastIntegrationTests.Tests.IntegreSQL/Infrastructure/IntegreSQL/IntegresSqlContainerManager.cs`
- Modify: `tests/FastIntegrationTests.Tests.IntegreSQL/Infrastructure/Base/AppServiceTestBase.cs`
- Modify: `tests/FastIntegrationTests.Tests.IntegreSQL/Infrastructure/Base/ComponentTestBase.cs`

- [ ] **Шаг 1: Миграции — IntegresSqlContainerManager**

В `IntegresSqlContainerManager.cs` заменить строки 77–79:

```csharp
// было:
var warmupCs = await initializer.CreateDatabaseGetConnectionString(
    IntegresSqlDefaults.SeedingOptions);
await initializer.RemoveDatabase(warmupCs);

// стало:
var migSw = System.Diagnostics.Stopwatch.StartNew();
var warmupCs = await initializer.CreateDatabaseGetConnectionString(
    IntegresSqlDefaults.SeedingOptions);
migSw.Stop();
Console.WriteLine($"##BENCH[migration]={migSw.ElapsedMilliseconds}");
await initializer.RemoveDatabase(warmupCs);
```

- [ ] **Шаг 2: Возврат в пул — AppServiceTestBase**

В `AppServiceTestBase.cs` заменить `DisposeAsync`:

```csharp
public virtual async Task DisposeAsync()
{
    await Context.DisposeAsync();
    await using var conn = new NpgsqlConnection(_connectionString);
    NpgsqlConnection.ClearPool(conn);
    var sw = System.Diagnostics.Stopwatch.StartNew();
    await _initializer.RemoveDatabase(_connectionString);
    sw.Stop();
    Console.WriteLine($"##BENCH[reset]={sw.ElapsedMilliseconds}");
}
```

- [ ] **Шаг 3: Возврат в пул — ComponentTestBase**

В `ComponentTestBase.cs` заменить `DisposeAsync`:

```csharp
public async Task DisposeAsync()
{
    Client?.Dispose();
    if (_factory is not null) await _factory.DisposeAsync();
    await using var conn = new NpgsqlConnection(_connectionString);
    NpgsqlConnection.ClearPool(conn);
    var sw = System.Diagnostics.Stopwatch.StartNew();
    await _initializer.RemoveDatabase(_connectionString);
    sw.Stop();
    Console.WriteLine($"##BENCH[reset]={sw.ElapsedMilliseconds}");
}
```

- [ ] **Шаг 4: Собрать тест-проект**

```bash
dotnet build tests/FastIntegrationTests.Tests.IntegreSQL --nologo -v minimal
```

Ожидается: `Build succeeded.`

- [ ] **Шаг 5: Запустить один тест-класс и проверить stdout**

```bash
dotnet test tests/FastIntegrationTests.Tests.IntegreSQL --filter "FullyQualifiedName~ProductServiceCrTests" --no-build -v normal 2>&1 | grep "##BENCH"
```

Ожидается строки вида:
```
##BENCH[migration]=1247
##BENCH[reset]=8
##BENCH[reset]=7
...
```

- [ ] **Шаг 6: Закоммитить**

```bash
git add tests/FastIntegrationTests.Tests.IntegreSQL/Infrastructure/IntegreSQL/IntegresSqlContainerManager.cs
git add tests/FastIntegrationTests.Tests.IntegreSQL/Infrastructure/Base/AppServiceTestBase.cs
git add tests/FastIntegrationTests.Tests.IntegreSQL/Infrastructure/Base/ComponentTestBase.cs
git commit -m "feat: ##BENCH-инструментирование IntegreSQL"
```

---

### Task 4: Инструментировать Respawn

**Files:**
- Modify: `tests/FastIntegrationTests.Tests.Respawn/Infrastructure/Fixtures/RespawnFixture.cs`

- [ ] **Шаг 1: Миграции и сброс — RespawnFixture**

Заменить `InitializeAsync` и `ResetAsync` в `RespawnFixture.cs`:

```csharp
public virtual async Task InitializeAsync()
{
    _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();
    await _container.StartAsync();
    ConnectionString = _container.GetConnectionString();

    var options = new DbContextOptionsBuilder<ShopDbContext>()
        .UseNpgsql(ConnectionString).Options;
    await using var ctx = new ShopDbContext(options);

    var migSw = System.Diagnostics.Stopwatch.StartNew();
    await ctx.Database.MigrateAsync();
    migSw.Stop();
    Console.WriteLine($"##BENCH[migration]={migSw.ElapsedMilliseconds}");

    await using var conn = new NpgsqlConnection(ConnectionString);
    await conn.OpenAsync();
    _respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
    {
        DbAdapter = DbAdapter.Postgres,
        SchemasToInclude = ["public"],
    });
}

public async Task ResetAsync()
{
    await using var conn = new NpgsqlConnection(ConnectionString);
    await conn.OpenAsync();
    var sw = System.Diagnostics.Stopwatch.StartNew();
    await _respawner.ResetAsync(conn);
    sw.Stop();
    Console.WriteLine($"##BENCH[reset]={sw.ElapsedMilliseconds}");
}
```

- [ ] **Шаг 2: Собрать тест-проект**

```bash
dotnet build tests/FastIntegrationTests.Tests.Respawn --nologo -v minimal
```

Ожидается: `Build succeeded.`

- [ ] **Шаг 3: Запустить один тест-класс и проверить stdout**

```bash
dotnet test tests/FastIntegrationTests.Tests.Respawn --filter "FullyQualifiedName~CategoryServiceCrRespawnTests" --no-build -v normal 2>&1 | grep "##BENCH"
```

Ожидается строки вида:
```
##BENCH[migration]=892
##BENCH[reset]=5
##BENCH[reset]=4
...
```

- [ ] **Шаг 4: Закоммитить**

```bash
git add tests/FastIntegrationTests.Tests.Respawn/Infrastructure/Fixtures/RespawnFixture.cs
git commit -m "feat: ##BENCH-инструментирование Respawn"
```

---

### Task 5: Инструментировать Testcontainers

**Files:**
- Modify: `tests/FastIntegrationTests.Tests.Testcontainers/Infrastructure/Factories/TestDbFactory.cs`

- [ ] **Шаг 1: Миграции — TestDbFactory**

Заменить блок `try` в `CreateAsync`:

```csharp
public async Task<ShopDbContext> CreateAsync(CancellationToken ct = default)
{
    var dbName = $"test_{Guid.NewGuid():N}";

    var csb = new NpgsqlConnectionStringBuilder(_fixture.ConnectionString)
    {
        Database = dbName
    };
    var options = new DbContextOptionsBuilder<ShopDbContext>()
        .UseNpgsql(csb.ConnectionString)
        .Options;

    var context = new ShopDbContext(options);
    try
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await context.Database.MigrateAsync(ct);
        sw.Stop();
        Console.WriteLine($"##BENCH[migration]={sw.ElapsedMilliseconds}");
    }
    catch
    {
        await context.DisposeAsync();
        throw;
    }
    return context;
}
```

- [ ] **Шаг 2: Собрать тест-проект**

```bash
dotnet build tests/FastIntegrationTests.Tests.Testcontainers --nologo -v minimal
```

Ожидается: `Build succeeded.`

- [ ] **Шаг 3: Запустить один тест-класс и проверить stdout**

```bash
dotnet test tests/FastIntegrationTests.Tests.Testcontainers --filter "FullyQualifiedName~CategoryServiceCrContainerTests" --no-build -v normal 2>&1 | grep "##BENCH"
```

Ожидается строки `##BENCH[migration]=NNN` — по одной на каждый тест, `##BENCH[reset]` отсутствуют.

- [ ] **Шаг 4: Закоммитить**

```bash
git add tests/FastIntegrationTests.Tests.Testcontainers/Infrastructure/Factories/TestDbFactory.cs
git commit -m "feat: ##BENCH-инструментирование Testcontainers"
```

---

### Task 6: Добавить stacked bar chart в отчёт

**Files:**
- Modify: `tools/BenchmarkRunner/Report/report-template.html`

- [ ] **Шаг 1: Добавить карточку для 4-го графика**

Вставить после строки 48 (после `</div>` третьего блока):

```html
  <div class="card">
    <h2>Состав времени — минимум и максимум миграций</h2>
    <p class="subtitle" id="subtitle-timing"></p>
    <canvas id="chart-timing"></canvas>
  </div>
```

- [ ] **Шаг 2: Добавить subtitle для нового графика**

В блоке `buildSubtitles()` после строки 71 добавить:

```javascript
      const m4 = data.results.find(r => r.scenario.scenarioName === 'migrations');
      if (m4) document.getElementById('subtitle-timing').textContent =
        `TEST_REPEAT=${m4.scenario.testRepeat}, потоков=${m4.scenario.maxParallelThreads}` +
        ` | Слои: миграции / сброс данных / бизнес-логика`;
```

- [ ] **Шаг 3: Добавить функцию buildTimingChart**

После строки `buildChart('chart-parallelism','parallelism', 'maxParallelThreads', 'MaxParallelThreads');` добавить:

```javascript
    (function buildTimingChart() {
      const MIN_M = 17, MAX_M = 117;
      const rows = data.results.filter(r =>
        r.scenario.scenarioName === 'migrations' &&
        (r.scenario.migrationCount === MIN_M || r.scenario.migrationCount === MAX_M)
      );
      if (!rows.length) return;

      const labels = [];
      const migData = [], resetData = [], bizData = [];
      const bizColors = [];

      for (const approach of APPROACHES) {
        for (const count of [MIN_M, MAX_M]) {
          const row = rows.find(r => r.scenario.approach === approach && r.scenario.migrationCount === count);
          labels.push(approach + ' (' + count + ')');
          const mig   = row ? +(row.migrationSeconds || 0).toFixed(2) : null;
          const reset = row ? +(row.resetSeconds     || 0).toFixed(2) : null;
          const biz   = row ? +Math.max(0, row.elapsedSeconds - (row.migrationSeconds || 0) - (row.resetSeconds || 0)).toFixed(2) : null;
          migData.push(mig);
          resetData.push(reset);
          bizData.push(biz);
          bizColors.push(COLORS[approach].border.replace('rgb', 'rgba').replace(')', ',0.7)'));
        }
      }

      new Chart(document.getElementById('chart-timing'), {
        type: 'bar',
        data: {
          labels,
          datasets: [
            { label: 'Миграции',      data: migData,   backgroundColor: 'rgba(251,146,60,0.85)', stack: 'a' },
            { label: 'Сброс данных',  data: resetData, backgroundColor: 'rgba(250,204,21,0.85)', stack: 'a' },
            { label: 'Бизнес-логика', data: bizData,   backgroundColor: bizColors,               stack: 'a' },
          ]
        },
        options: {
          responsive: true,
          interaction: { mode: 'index', intersect: false },
          plugins: {
            legend: { position: 'top' },
            tooltip: { callbacks: { label: ctx => ctx.dataset.label + ': ' + ctx.parsed.y + ' с' } }
          },
          scales: {
            x: { stacked: true },
            y: { stacked: true, title: { display: true, text: 'Время (секунды)' }, beginAtZero: true }
          }
        }
      });
    })();
```

- [ ] **Шаг 4: Проверить шаблон локально**

Создать временный HTML-файл с тестовыми данными и открыть в браузере:

```bash
# Просто убедиться что шаблон не сломан синтаксически
node -e "const fs=require('fs'); const h=fs.readFileSync('tools/BenchmarkRunner/Report/report-template.html','utf8'); console.log('OK, size:', h.length)"
```

Ожидается: `OK, size: NNNN` без ошибок.

- [ ] **Шаг 5: Закоммитить**

```bash
git add tools/BenchmarkRunner/Report/report-template.html
git commit -m "feat: stacked bar chart состава времени по подходам"
```

---

### Task 7: Финальная верификация

- [ ] **Шаг 1: Собрать все проекты**

```bash
dotnet build --nologo -v minimal
```

Ожидается: `Build succeeded.` без ошибок.

- [ ] **Шаг 2: Запустить короткий бенчмарк для проверки e2e**

```bash
dotnet run --project tools/BenchmarkRunner -- --threads 4 --repeat 1 --timeout 20
```

Ожидается: все 42 точки собираются, в `benchmark-results/report.html` появляется 4-й график. Открыть отчёт и убедиться что stacked bars отображаются корректно.

```bash
start benchmark-results/report.html
```

- [ ] **Шаг 3: Обновить CLAUDE.md**

В секции `## Идеи для развития бенчмарка` добавить запись о реализованной фиче:

```markdown
### ~~Декомпозиция времени: миграции / сброс / бизнес-логика~~ ✓ Реализовано

> Спроектировано в `docs/superpowers/specs/2026-04-25-migration-timing-design.md`.
> Stacked bar chart в отчёте показывает состав времени при 17 и 117 миграциях.
> Инструментирование через `##BENCH[migration]=` и `##BENCH[reset]=` в stdout тест-инфраструктуры.
```

- [ ] **Шаг 4: Закоммитить CLAUDE.md и спек**

```bash
git add CLAUDE.md docs/superpowers/specs/2026-04-25-migration-timing-design.md docs/superpowers/plans/2026-04-25-migration-timing.md
git commit -m "docs: фиксируем спек и план декомпозиции времени"
```
