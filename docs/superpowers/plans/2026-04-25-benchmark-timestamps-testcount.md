# Benchmark Timestamps & Test Count Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Добавить таймстемпы в имена файлов отчётов и показывать реальное количество тестов вместо TEST_REPEAT.

**Architecture:** `BenchmarkReport` получает поле `BaseTestCount`; `ReportGenerator` хранит таймстемп с момента создания и использует его во всех сохранениях; `report-template.html` принимает `BASE_TEST_COUNT` из JSON и применяет трансформацию к оси X Сценария 2 и всем субтайтлам.

**Tech Stack:** .NET 8, C# 12 records, Chart.js v4, JavaScript (ES6).

---

### Task 1: BaseTestCount — модель, константа, Program.cs

**Files:**
- Modify: `tools/BenchmarkRunner/Models/BenchmarkReport.cs`
- Modify: `tools/BenchmarkRunner/Program.cs`

- [ ] **Шаг 1: Подсчитать реальное количество тест-методов**

```bash
dotnet test tests/FastIntegrationTests.Tests.IntegreSQL --list-tests --no-build 2>/dev/null | grep -c "::"
```

Ожидается: число вида `340`. Запомнить это значение — оно понадобится в шаге 3.

Если `--no-build` не работает — сначала собрать:
```bash
dotnet build tests/FastIntegrationTests.Tests.IntegreSQL --nologo -v minimal
dotnet test tests/FastIntegrationTests.Tests.IntegreSQL --list-tests 2>/dev/null | grep -c "::"
```

- [ ] **Шаг 2: Обновить BenchmarkReport.cs**

```csharp
// tools/BenchmarkRunner/Models/BenchmarkReport.cs
namespace BenchmarkRunner.Models;

/// <summary>
/// Отчёт о результатах бенчмарков.
/// </summary>
/// <param name="GeneratedAt">Дата и время создания отчёта</param>
/// <param name="MachineName">Имя машины, на которой запускались тесты</param>
/// <param name="BaseTestCount">Базовое количество тест-методов в одном подходе (хардкод)</param>
/// <param name="Results">Коллекция результатов бенчмарков</param>
public record BenchmarkReport(
    DateTime GeneratedAt,
    string MachineName,
    int BaseTestCount,
    IReadOnlyList<BenchmarkResult> Results
);
```

- [ ] **Шаг 3: Обновить Program.cs — добавить константу и передать везде**

В начало файла после объявления `defaultThreads`, `defaultRepeat`, `timeoutMinutes` добавить константу (вместо `340` подставить число из шага 1):

```csharp
// хардкод — обновить при добавлении/удалении тест-методов в тест-проектах
// проверить: dotnet test tests/FastIntegrationTests.Tests.IntegreSQL --list-tests | grep -c "::"
const int BaseTestCount = 340;
```

Затем обновить строку создания `BenchmarkReport` в двух местах:

```csharp
// В функции RunOrAbort (строка ~123):
reportGenerator.SaveJson(new BenchmarkReport(DateTime.UtcNow, Environment.MachineName, BaseTestCount, results));

// В финальном отчёте (строка ~110):
var report = new BenchmarkReport(DateTime.UtcNow, Environment.MachineName, BaseTestCount, results);
```

- [ ] **Шаг 4: Собрать и проверить компиляцию**

```bash
dotnet build tools/BenchmarkRunner --nologo -v minimal
```

Ожидается: `Build succeeded. 0 Warning(s). 0 Error(s).`

- [ ] **Шаг 5: Закоммитить**

```bash
git add tools/BenchmarkRunner/Models/BenchmarkReport.cs tools/BenchmarkRunner/Program.cs
git commit -m "feat: BaseTestCount в BenchmarkReport и Program.cs"
```

---

### Task 2: Таймстемп в именах файлов (ReportGenerator)

**Files:**
- Modify: `tools/BenchmarkRunner/Report/ReportGenerator.cs`
- Modify: `tools/BenchmarkRunner/Program.cs`

- [ ] **Шаг 1: Обновить ReportGenerator.cs**

Полная замена файла:

```csharp
// tools/BenchmarkRunner/Report/ReportGenerator.cs
using System.Text.Json;
using BenchmarkRunner.Models;

namespace BenchmarkRunner.Report;

/// <summary>Генерирует HTML отчёт и results.json из данных бенчмарка.</summary>
public class ReportGenerator
{
    private readonly string _templatePath;
    private readonly string _outputDir;
    private readonly string _timestamp;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented        = true,
    };

    /// <summary>Инициализирует генератор с корневой директорией репозитория.</summary>
    public ReportGenerator(string repoRoot)
    {
        _templatePath = Path.Combine(AppContext.BaseDirectory, "Report", "report-template.html");
        _outputDir    = Path.Combine(repoRoot, "benchmark-results");
        _timestamp    = DateTime.Now.ToString("yyyyMMdd-HHmmss");
    }

    /// <summary>Сохраняет промежуточный results.json после каждой точки данных.</summary>
    public void SaveJson(BenchmarkReport report)
    {
        Directory.CreateDirectory(_outputDir);
        var json = JsonSerializer.Serialize(report, JsonOptions);
        File.WriteAllText(Path.Combine(_outputDir, $"results-{_timestamp}.json"), json);
    }

    /// <summary>Сериализует отчёт в JSON, инлайнит в HTML шаблон, сохраняет оба файла.</summary>
    /// <returns>Абсолютный путь к сохранённому HTML файлу.</returns>
    public string Generate(BenchmarkReport report)
    {
        SaveJson(report);
        Console.WriteLine($"\n[REPORT] results-{_timestamp}.json saved");

        var json     = JsonSerializer.Serialize(report, JsonOptions);
        var template = File.ReadAllText(_templatePath);
        var html     = template.Replace("/*INJECT_JSON*/", json);
        var htmlPath = Path.Combine(_outputDir, $"report-{_timestamp}.html");
        File.WriteAllText(htmlPath, html);
        Console.WriteLine($"[REPORT] report-{_timestamp}.html saved: {htmlPath}");
        return htmlPath;
    }
}
```

- [ ] **Шаг 2: Обновить Program.cs — использовать путь из Generate()**

Заменить строки финального отчёта (около строк 110–114):

```csharp
// было:
var report = new BenchmarkReport(DateTime.UtcNow, Environment.MachineName, BaseTestCount, results);
reportGenerator.Generate(report);

Console.WriteLine("\n=== Done! ===");
Console.WriteLine("Open: benchmark-results/report.html");

// стало:
var report  = new BenchmarkReport(DateTime.UtcNow, Environment.MachineName, BaseTestCount, results);
var htmlPath = reportGenerator.Generate(report);

Console.WriteLine("\n=== Done! ===");
Console.WriteLine($"Open: {Path.GetRelativePath(repoRoot, htmlPath)}");
```

- [ ] **Шаг 3: Собрать и проверить**

```bash
dotnet build tools/BenchmarkRunner --nologo -v minimal
```

Ожидается: `Build succeeded. 0 Warning(s). 0 Error(s).`

- [ ] **Шаг 4: Закоммитить**

```bash
git add tools/BenchmarkRunner/Report/ReportGenerator.cs tools/BenchmarkRunner/Program.cs
git commit -m "feat: таймстемп в именах файлов отчёта"
```

---

### Task 3: Обновить report-template.html

**Files:**
- Modify: `tools/BenchmarkRunner/Report/report-template.html`

- [ ] **Шаг 1: Добавить BASE_TEST_COUNT после объявления APPROACHES**

Найти строку:
```javascript
    const APPROACHES = ['IntegreSQL', 'Respawn', 'Testcontainers'];
```

Добавить после неё:
```javascript
    const BASE_TEST_COUNT = data.baseTestCount;
```

- [ ] **Шаг 2: Добавить параметр xTransform в buildChart**

Найти объявление функции:
```javascript
    function buildChart(canvasId, scenarioName, xField, xLabel) {
```

Заменить на:
```javascript
    function buildChart(canvasId, scenarioName, xField, xLabel, xTransform = x => x) {
```

Найти строку `data: { labels: xValues, datasets },` и заменить на:
```javascript
        data: { labels: xValues.map(xTransform), datasets },
```

- [ ] **Шаг 3: Обновить вызов buildChart для Сценария 2**

Найти:
```javascript
    buildChart('chart-scale',      'scale',      'testRepeat',          'TEST_REPEAT');
```

Заменить на:
```javascript
    buildChart('chart-scale',      'scale',      'testRepeat',          'Количество тестов', x => x * BASE_TEST_COUNT);
```

- [ ] **Шаг 4: Обновить субтайтлы**

Найти блок `buildSubtitles()` и заменить все четыре `textContent` присваивания:

```javascript
    (function buildSubtitles() {
      const m1 = data.results.find(r => r.scenario.scenarioName === 'migrations');
      if (m1) document.getElementById('subtitle-migrations').textContent =
        `${m1.scenario.testRepeat * BASE_TEST_COUNT} тестов, потоков=${m1.scenario.maxParallelThreads}` +
        ` | Ось X: количество миграций в проекте`;

      const m2 = data.results.find(r => r.scenario.scenarioName === 'scale');
      if (m2) document.getElementById('subtitle-scale').textContent =
        `${m2.scenario.migrationCount} миграций, потоков=${m2.scenario.maxParallelThreads}` +
        ` | Ось X: количество тестов`;

      const m3 = data.results.find(r => r.scenario.scenarioName === 'parallelism');
      if (m3) document.getElementById('subtitle-parallelism').textContent =
        `${m3.scenario.migrationCount} миграций, ${m3.scenario.testRepeat * BASE_TEST_COUNT} тестов` +
        ` | Ось X: MaxParallelThreads`;

      if (m1) document.getElementById('subtitle-timing').textContent =
        `${m1.scenario.testRepeat * BASE_TEST_COUNT} тестов, потоков=${m1.scenario.maxParallelThreads}` +
        ` | Слои: миграции / сброс данных / бизнес-логика`;
    })();
```

- [ ] **Шаг 5: Проверить синтаксис**

```bash
node -e "const fs=require('fs'); const h=fs.readFileSync('tools/BenchmarkRunner/Report/report-template.html','utf8'); console.log('OK, size:', h.length)"
```

Ожидается: `OK, size: NNNN` без ошибок.

- [ ] **Шаг 6: Закоммитить**

```bash
git add tools/BenchmarkRunner/Report/report-template.html
git commit -m "feat: BASE_TEST_COUNT и количество тестов в субтайтлах отчёта"
```

---

### Task 4: Обновить CLAUDE.md и документацию

**Files:**
- Modify: `CLAUDE.md`
- Modify: `docs/superpowers/specs/2026-04-25-benchmark-timestamps-testcount-design.md` (коммит)
- Modify: `docs/superpowers/plans/2026-04-25-benchmark-timestamps-testcount.md` (этот файл)

- [ ] **Шаг 1: Добавить примечание в CLAUDE.md**

В секции `## Benchmark Runner`, после таблицы с аргументами (`| --timeout N | ...`), добавить:

```markdown
> **Важно:** константа `BaseTestCount` в `tools/BenchmarkRunner/Program.cs` — хардкод.
> При добавлении или удалении тест-методов обновить вручную.
> Актуальное значение: `dotnet test tests/FastIntegrationTests.Tests.IntegreSQL --list-tests 2>/dev/null | grep -c "::"`
```

- [ ] **Шаг 2: Финальная сборка**

```bash
dotnet build --nologo -v minimal
```

Ожидается: `Build succeeded. 0 Warning(s). 0 Error(s).`

- [ ] **Шаг 3: Закоммитить всё**

```bash
git add CLAUDE.md docs/superpowers/specs/2026-04-25-benchmark-timestamps-testcount-design.md docs/superpowers/plans/2026-04-25-benchmark-timestamps-testcount.md
git commit -m "docs: таймстемпы и BaseTestCount — спек, план, CLAUDE.md"
```
