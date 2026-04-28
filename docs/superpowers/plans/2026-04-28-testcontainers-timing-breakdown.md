# Testcontainers Timing Breakdown Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Добавить детализацию времени по компонентам — исправить stacked bar (пересчитать в среднее на тест), добавить два новых Testcontainers-графика ("один контейнер" и "весь прогон"), проинструментировать клонирование БД (IntegreSQL) и старт контейнеров (Testcontainers, Respawn).

**Architecture:** Добавляем четыре новых `##BENCH[X]=` маркера в тест-инфраструктуру; парсер в `TestRunner` суммирует их в `BenchmarkResult`; шаблон HTML читает новые поля и строит три изменённых/новых графика. Вся цепочка: тест → stdout → BENCH_LOG_FILE → `ParseBenchLines` → `BenchmarkResult` → JSON → HTML.

**Tech Stack:** C# (.NET 8), xUnit, Chart.js 4, HTML/JS (vanilla)

---

## File Map

| Файл | Изменение |
|---|---|
| `tools/BenchmarkRunner/Models/BenchmarkResult.cs` | +`ContainerSeconds`, +`CloneSeconds` |
| `tools/BenchmarkRunner/Runner/TestRunner.cs` | парсинг `container` и `clone` маркеров |
| `tests/FastIntegrationTests.Tests.IntegreSQL/Infrastructure/Base/AppServiceTestBase.cs` | `##BENCH[clone]=` в `InitializeAsync` |
| `tests/FastIntegrationTests.Tests.IntegreSQL/Infrastructure/Base/ComponentTestBase.cs` | `##BENCH[clone]=` в `InitializeAsync` |
| `tests/FastIntegrationTests.Tests.Testcontainers/Infrastructure/Fixtures/ContainerFixture.cs` | `##BENCH[container]=` в `InitializeAsync` |
| `tests/FastIntegrationTests.Tests.Respawn/Infrastructure/RespawnContainerManager.cs` | `##BENCH[container]=` в `StartAsync` |
| `tools/BenchmarkRunner/Report/report-template.html` | фикс timing chart + 2 новые карты |

---

### Task 1: Расширить BenchmarkResult и TestRunner

**Files:**
- Modify: `tools/BenchmarkRunner/Models/BenchmarkResult.cs`
- Modify: `tools/BenchmarkRunner/Runner/TestRunner.cs`

- [ ] **Шаг 1: Обновить BenchmarkResult**

Заменить содержимое `tools/BenchmarkRunner/Models/BenchmarkResult.cs`:

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
/// <param name="ContainerSeconds">Суммарное время старта контейнеров в секундах</param>
/// <param name="CloneSeconds">Суммарное время клонирования БД в секундах (IntegreSQL)</param>
/// <param name="Success">Успешное ли выполнение</param>
public record BenchmarkResult(
    BenchmarkScenario Scenario,
    double ElapsedSeconds,
    double MigrationSeconds,
    double ResetSeconds,
    double ContainerSeconds,
    double CloneSeconds,
    bool Success
);
```

- [ ] **Шаг 2: Обновить ParseBenchLines в TestRunner**

Найти метод `ParseBenchLines` в `tools/BenchmarkRunner/Runner/TestRunner.cs` и заменить его:

```csharp
private static (long MigrationMs, long ResetMs, long ContainerMs, long CloneMs) ParseBenchLines(string output)
{
    long migrationMs = 0, resetMs = 0, containerMs = 0, cloneMs = 0;
    foreach (var line in output.Split('\n'))
    {
        var t = line.TrimEnd('\r');
        if (t.StartsWith("##BENCH[migration]=") &&
            long.TryParse(t["##BENCH[migration]=".Length..], out var m))
            migrationMs += m;
        else if (t.StartsWith("##BENCH[reset]=") &&
            long.TryParse(t["##BENCH[reset]=".Length..], out var r))
            resetMs += r;
        else if (t.StartsWith("##BENCH[container]=") &&
            long.TryParse(t["##BENCH[container]=".Length..], out var c))
            containerMs += c;
        else if (t.StartsWith("##BENCH[clone]=") &&
            long.TryParse(t["##BENCH[clone]=".Length..], out var cl))
            cloneMs += cl;
    }
    return (migrationMs, resetMs, containerMs, cloneMs);
}
```

- [ ] **Шаг 3: Обновить RunTest в TestRunner**

Найти метод `RunTest` и заменить две строки в конце метода (после `WaitForRyukToStop()`):

```csharp
var (migrationMs, resetMs, containerMs, cloneMs) = ParseBenchLines(benchContent);
return (sw.Elapsed.TotalSeconds, code == 0, output, migrationMs / 1000.0, resetMs / 1000.0, containerMs / 1000.0, cloneMs / 1000.0);
```

И обновить сигнатуру возвращаемого типа метода:

```csharp
private (double Elapsed, bool Success, string Output, double MigrationSeconds, double ResetSeconds, double ContainerSeconds, double CloneSeconds) RunTest(BenchmarkScenario scenario)
```

- [ ] **Шаг 4: Обновить вызовы RunTest в Warmup и Run**

В методах `Warmup` и `Run` обновить деструктуризацию и конструктор `BenchmarkResult`:

```csharp
var (elapsed, success, output, migrationSeconds, resetSeconds, containerSeconds, cloneSeconds) = RunTest(scenario);
// ...
return new BenchmarkResult(scenario, elapsed, migrationSeconds, resetSeconds, containerSeconds, cloneSeconds, success);
```

- [ ] **Шаг 5: Проверить сборку**

```bash
dotnet build tools/BenchmarkRunner --nologo -v minimal
```

Ожидание: `Build succeeded. 0 Warning(s). 0 Error(s).`

- [ ] **Шаг 6: Коммит**

```bash
git add tools/BenchmarkRunner/Models/BenchmarkResult.cs tools/BenchmarkRunner/Runner/TestRunner.cs
git commit -m "feat: добавить ContainerSeconds и CloneSeconds в BenchmarkResult и парсер"
```

---

### Task 2: `##BENCH[clone]=` в IntegreSQL

**Files:**
- Modify: `tests/FastIntegrationTests.Tests.IntegreSQL/Infrastructure/Base/AppServiceTestBase.cs`
- Modify: `tests/FastIntegrationTests.Tests.IntegreSQL/Infrastructure/Base/ComponentTestBase.cs`

- [ ] **Шаг 1: AppServiceTestBase — добавить замер клонирования**

В `AppServiceTestBase.InitializeAsync()` обернуть вызов `CreateDatabaseGetConnectionString`:

```csharp
public virtual async Task InitializeAsync()
{
    var state = await IntegresSqlContainerManager.GetStateAsync();
    _initializer = state.Initializer;
    var sw = System.Diagnostics.Stopwatch.StartNew();
    _connectionString = await _initializer.CreateDatabaseGetConnectionString<ShopDbContext>(
        IntegresSqlDefaults.SeedingOptions);
    sw.Stop();
    BenchmarkLogger.Write("clone", sw.ElapsedMilliseconds);
    var options = new DbContextOptionsBuilder<ShopDbContext>()
        .UseNpgsql(_connectionString).Options;
    Context = new ShopDbContext(options);
}
```

- [ ] **Шаг 2: ComponentTestBase — добавить замер клонирования**

В `ComponentTestBase.InitializeAsync()` аналогично:

```csharp
public async Task InitializeAsync()
{
    var state = await IntegresSqlContainerManager.GetStateAsync();
    _initializer = state.Initializer;
    var sw = System.Diagnostics.Stopwatch.StartNew();
    _connectionString = await _initializer.CreateDatabaseGetConnectionString<ShopDbContext>(
        IntegresSqlDefaults.SeedingOptions);
    sw.Stop();
    BenchmarkLogger.Write("clone", sw.ElapsedMilliseconds);
    _factory = new TestWebApplicationFactory(_connectionString);
    Client = _factory.CreateClient();
}
```

- [ ] **Шаг 3: Проверить сборку**

```bash
dotnet build tests/FastIntegrationTests.Tests.IntegreSQL --nologo -v minimal
```

Ожидание: `Build succeeded. 0 Warning(s). 0 Error(s).`

- [ ] **Шаг 4: Коммит**

```bash
git add tests/FastIntegrationTests.Tests.IntegreSQL/Infrastructure/Base/AppServiceTestBase.cs
git add tests/FastIntegrationTests.Tests.IntegreSQL/Infrastructure/Base/ComponentTestBase.cs
git commit -m "feat: ##BENCH[clone]= для замера клонирования БД в IntegreSQL"
```

---

### Task 3: `##BENCH[container]=` в Testcontainers

**Files:**
- Modify: `tests/FastIntegrationTests.Tests.Testcontainers/Infrastructure/Fixtures/ContainerFixture.cs`

- [ ] **Шаг 1: Добавить замер старта контейнера**

В `ContainerFixture.InitializeAsync()` обернуть создание сети и старт контейнера:

```csharp
public async Task InitializeAsync()
{
    var sw = System.Diagnostics.Stopwatch.StartNew();
    _network = new NetworkBuilder().Build();
    await _network.CreateAsync();
    _container = new PostgreSqlBuilder()
        .WithNetwork(_network)
        .WithImage("postgres:16-alpine")
        .WithCommand(
            "-c", "fsync=off",
            "-c", "synchronous_commit=off",
            "-c", "full_page_writes=off",
            "-c", "shared_buffers=128MB"
        )
        .Build();
    await _container.StartAsync();
    sw.Stop();
    BenchmarkLogger.Write("container", sw.ElapsedMilliseconds);
    ConnectionString = _container.GetConnectionString();
}
```

- [ ] **Шаг 2: Проверить сборку**

```bash
dotnet build tests/FastIntegrationTests.Tests.Testcontainers --nologo -v minimal
```

Ожидание: `Build succeeded. 0 Warning(s). 0 Error(s).`

- [ ] **Шаг 3: Коммит**

```bash
git add tests/FastIntegrationTests.Tests.Testcontainers/Infrastructure/Fixtures/ContainerFixture.cs
git commit -m "feat: ##BENCH[container]= для замера старта контейнера в Testcontainers"
```

---

### Task 4: `##BENCH[container]=` в Respawn

**Files:**
- Modify: `tests/FastIntegrationTests.Tests.Respawn/Infrastructure/RespawnContainerManager.cs`

- [ ] **Шаг 1: Добавить замер старта контейнера**

В `RespawnContainerManager.StartAsync()` обернуть `container.StartAsync()`:

```csharp
private static async Task<PostgreSqlContainer> StartAsync()
{
    var container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithCommand(
            "-c", "max_connections=500",
            "-c", "fsync=off",
            "-c", "synchronous_commit=off",
            "-c", "full_page_writes=off",
            "-c", "shared_buffers=128MB"
        )
        .Build();
    var sw = System.Diagnostics.Stopwatch.StartNew();
    await container.StartAsync();
    sw.Stop();
    BenchmarkLogger.Write("container", sw.ElapsedMilliseconds);
    return container;
}
```

- [ ] **Шаг 2: Проверить сборку**

```bash
dotnet build tests/FastIntegrationTests.Tests.Respawn --nologo -v minimal
```

Ожидание: `Build succeeded. 0 Warning(s). 0 Error(s).`

- [ ] **Шаг 3: Коммит**

```bash
git add tests/FastIntegrationTests.Tests.Respawn/Infrastructure/RespawnContainerManager.cs
git commit -m "feat: ##BENCH[container]= для замера старта контейнера в Respawn"
```

---

### Task 5: Исправить timing chart в HTML-шаблоне

**Files:**
- Modify: `tools/BenchmarkRunner/Report/report-template.html`

Это задание меняет существующий stacked bar: пересчитывает значения в "среднее на тест (мс)", добавляет слои `container` и `clone`, убирает "бизнес-логику".

- [ ] **Шаг 1: Переименовать карту в HTML**

Найти и заменить:
```html
<h2>Состав времени — минимум и максимум миграций</h2>
```
На:
```html
<h2>Измеренный overhead на тест (мс) — минимум и максимум миграций</h2>
```

- [ ] **Шаг 2: Обновить subtitle timing в buildSubtitles()**

Найти строку:
```js
` | Слои: миграции / сброс данных / бизнес-логика`;
```
Заменить на:
```js
` | Среднее на тест (мс): контейнер / миграции / клон БД / сброс`;
```

- [ ] **Шаг 3: Заменить buildTimingChart**

Найти весь блок `(function buildTimingChart() { ... })();` и заменить на:

```js
(function buildTimingChart() {
  const MIN_M = 17, MAX_M = 117;
  const rows = data.results.filter(r =>
    r.scenario.scenarioName === 'migrations' &&
    (r.scenario.migrationCount === MIN_M || r.scenario.migrationCount === MAX_M)
  );
  if (!rows.length) return;

  const testCount = rows[0].scenario.classScale * BASE_TEST_COUNT;
  const toMs = s => +((s || 0) * 1000 / testCount).toFixed(1);

  for (const approach of APPROACHES) {
    for (const count of [MIN_M, MAX_M]) {
      const row = rows.find(r => r.scenario.approach === approach && r.scenario.migrationCount === count);
      timingItems.push({
        label:     approach + ' (' + count + ')',
        approach,
        container: row ? toMs(row.containerSeconds) : null,
        migration: row ? toMs(row.migrationSeconds) : null,
        clone:     row ? toMs(row.cloneSeconds)     : null,
        reset:     row ? toMs(row.resetSeconds)     : null,
      });
    }
  }

  timingChart = new Chart(document.getElementById('chart-timing'), {
    type: 'bar',
    data: {
      labels:   timingItems.map(i => i.label),
      datasets: [
        { label: 'Контейнер',    data: timingItems.map(i => i.container), backgroundColor: 'rgba(251,146,60,0.85)', stack: 'a' },
        { label: 'Миграции',     data: timingItems.map(i => i.migration), backgroundColor: 'rgba(99,102,241,0.85)',  stack: 'a' },
        { label: 'Клон БД',      data: timingItems.map(i => i.clone),     backgroundColor: 'rgba(168,85,247,0.85)',  stack: 'a' },
        { label: 'Сброс данных', data: timingItems.map(i => i.reset),     backgroundColor: 'rgba(250,204,21,0.85)', stack: 'a' },
      ]
    },
    options: {
      responsive: true,
      interaction: { mode: 'index', intersect: false },
      plugins: {
        legend: { position: 'top' },
        tooltip: { callbacks: { label: ctx => ctx.dataset.label + ': ' + ctx.parsed.y + ' мс' } }
      },
      scales: {
        x: { stacked: true },
        y: { stacked: true, title: { display: true, text: 'мс / тест (среднее)' }, beginAtZero: true }
      }
    }
  });
})();
```

- [ ] **Шаг 4: Обновить updateApproachVisibility для timing chart**

Найти блок `// Stacked bar: пересобираем...` внутри `updateApproachVisibility` и заменить его:

```js
// Stacked bar: пересобираем labels и данные, фильтруя по видимым подходам
if (timingChart) {
  const visible = timingItems.filter(item => visibleApproaches.has(item.approach));
  timingChart.data.labels             = visible.map(i => i.label);
  timingChart.data.datasets[0].data   = visible.map(i => i.container);
  timingChart.data.datasets[1].data   = visible.map(i => i.migration);
  timingChart.data.datasets[2].data   = visible.map(i => i.clone);
  timingChart.data.datasets[3].data   = visible.map(i => i.reset);
  timingChart.update();
}
```

- [ ] **Шаг 5: Коммит**

```bash
git add tools/BenchmarkRunner/Report/report-template.html
git commit -m "feat: исправить timing chart — per-test average (мс), 4 слоя"
```

---

### Task 6: Добавить карту "Testcontainers — один контейнер"

**Files:**
- Modify: `tools/BenchmarkRunner/Report/report-template.html`

- [ ] **Шаг 1: Добавить HTML-карту**

После карты `chart-timing` добавить:

```html
<div class="card">
  <h2>Testcontainers — один контейнер (мс)</h2>
  <p class="subtitle" id="subtitle-one-container"></p>
  <canvas id="chart-one-container"></canvas>
</div>
```

- [ ] **Шаг 2: Добавить JS — buildOneContainerChart**

В конце `<script>`, после `buildTimingChart` IIFE и до `updateApproachVisibility`, добавить:

```js
const TESTCONTAINERS_BASE_CLASSES = 14; // 7 сущностей × 2 (Service + API)

(function buildOneContainerChart() {
  const MIN_M = 17, MAX_M = 117;
  const rows = data.results
    .filter(r =>
      r.scenario.scenarioName === 'migrations' &&
      r.scenario.approach === 'Testcontainers' &&
      (r.scenario.migrationCount === MIN_M || r.scenario.migrationCount === MAX_M)
    )
    .sort((a, b) => a.scenario.migrationCount - b.scenario.migrationCount);
  if (!rows.length) return;

  const classScale        = rows[0].scenario.classScale;
  const testCount         = classScale * BASE_TEST_COUNT;
  const containerCount    = classScale * TESTCONTAINERS_BASE_CLASSES;
  const testsPerContainer = BASE_TEST_COUNT / TESTCONTAINERS_BASE_CLASSES;

  document.getElementById('subtitle-one-container').textContent =
    `≈${Math.round(testsPerContainer)} тестов на контейнер, потоков=${rows[0].scenario.maxParallelThreads}` +
    ` | overhead одного контейнера (мс)`;

  const labels      = rows.map(r => r.scenario.migrationCount + ' мig');
  const contData    = rows.map(r => +((r.containerSeconds || 0) * 1000 / containerCount).toFixed(1));
  const migData     = rows.map(r => +((r.migrationSeconds || 0) * 1000 / testCount * testsPerContainer).toFixed(1));
  const resetData   = rows.map(r => +((r.resetSeconds     || 0) * 1000 / testCount * testsPerContainer).toFixed(1));

  new Chart(document.getElementById('chart-one-container'), {
    type: 'bar',
    data: {
      labels,
      datasets: [
        { label: 'Старт контейнера',          data: contData,  backgroundColor: 'rgba(251,146,60,0.85)', stack: 'a' },
        { label: 'Миграции (×N тестов)',       data: migData,   backgroundColor: 'rgba(99,102,241,0.85)',  stack: 'a' },
        { label: 'EnsureDeleted (×N тестов)',  data: resetData, backgroundColor: 'rgba(250,204,21,0.85)', stack: 'a' },
      ]
    },
    options: {
      responsive: true,
      interaction: { mode: 'index', intersect: false },
      plugins: {
        legend: { position: 'top' },
        tooltip: { callbacks: { label: ctx => ctx.dataset.label + ': ' + ctx.parsed.y + ' мс' } }
      },
      scales: {
        x: { stacked: true, title: { display: true, text: 'Количество миграций' } },
        y: { stacked: true, title: { display: true, text: 'мс' }, beginAtZero: true }
      }
    }
  });
})();
```

- [ ] **Шаг 3: Коммит**

```bash
git add tools/BenchmarkRunner/Report/report-template.html
git commit -m "feat: добавить карту 'Testcontainers — один контейнер'"
```

---

### Task 7: Добавить карту "Testcontainers — весь прогон"

**Files:**
- Modify: `tools/BenchmarkRunner/Report/report-template.html`

- [ ] **Шаг 1: Добавить HTML-карту**

После карты `chart-one-container` добавить:

```html
<div class="card">
  <h2>Testcontainers — весь прогон (с)</h2>
  <p class="subtitle" id="subtitle-whole-run"></p>
  <canvas id="chart-whole-run"></canvas>
</div>
```

- [ ] **Шаг 2: Добавить JS — buildWholeRunChart**

В конце `<script>`, после `buildOneContainerChart` IIFE, добавить:

```js
(function buildWholeRunChart() {
  const rows = data.results
    .filter(r => r.scenario.scenarioName === 'migrations' && r.scenario.approach === 'Testcontainers')
    .sort((a, b) => a.scenario.migrationCount - b.scenario.migrationCount);
  if (!rows.length) return;

  document.getElementById('subtitle-whole-run').textContent =
    `${rows[0].scenario.classScale * BASE_TEST_COUNT} тестов, потоков=${rows[0].scenario.maxParallelThreads}` +
    ` | суммарное время по всем потокам (с) vs wall-clock`;

  const labels    = rows.map(r => r.scenario.migrationCount + ' мig');
  const contData  = rows.map(r => +((r.containerSeconds || 0).toFixed(1)));
  const migData   = rows.map(r => +((r.migrationSeconds || 0).toFixed(1)));
  const resetData = rows.map(r => +((r.resetSeconds     || 0).toFixed(1)));
  const elapsed   = rows.map(r => +(r.elapsedSeconds.toFixed(1)));

  new Chart(document.getElementById('chart-whole-run'), {
    type: 'bar',
    data: {
      labels,
      datasets: [
        { type: 'bar',  label: 'Контейнеры',          data: contData,  backgroundColor: 'rgba(251,146,60,0.85)', stack: 'a' },
        { type: 'bar',  label: 'Миграции',              data: migData,   backgroundColor: 'rgba(99,102,241,0.85)',  stack: 'a' },
        { type: 'bar',  label: 'Сброс (EnsureDeleted)', data: resetData, backgroundColor: 'rgba(250,204,21,0.85)', stack: 'a' },
        { type: 'line', label: 'Wall-clock (elapsed)',  data: elapsed,
          borderColor: 'rgb(239,68,68)', backgroundColor: 'rgba(239,68,68,0.1)',
          borderWidth: 2, pointRadius: 5, tension: 0.15, fill: false },
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
        x: { stacked: true, title: { display: true, text: 'Количество миграций' } },
        y: { stacked: true, title: { display: true, text: 'Время (секунды)' }, beginAtZero: true }
      }
    }
  });
})();
```

- [ ] **Шаг 3: Коммит**

```bash
git add tools/BenchmarkRunner/Report/report-template.html
git commit -m "feat: добавить карту 'Testcontainers — весь прогон'"
```
