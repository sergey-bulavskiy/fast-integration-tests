# Benchmark Runner Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Создать .NET консольное приложение `tools/BenchmarkRunner`, которое прогоняет три подхода к изоляции тестов через три сценария и генерирует `benchmark-results/report.html` с интерактивными Chart.js графиками.

**Architecture:** `Program.cs` оркестрирует сценарии. `TestRunner` запускает `dotnet test` через `Process` и меряет wall-clock время. `MigrationManager` пишет и удаляет фейковые `.cs` файлы миграций напрямую в папку Infrastructure. `ReportGenerator` инлайнит JSON в HTML шаблон — файл работает по `file://` без сервера.

**Tech Stack:** .NET 8 console app, System.Text.Json (встроен), System.Diagnostics.Process, Chart.js 4 (CDN)

---

## Карта файлов

```
tools/
└── BenchmarkRunner/
    ├── BenchmarkRunner.csproj          — проект, копирует шаблон в output dir
    ├── Program.cs                      — оркестрация трёх сценариев
    ├── Models/
    │   ├── BenchmarkScenario.cs        — описание одного запуска (record)
    │   ├── BenchmarkResult.cs          — результат одного запуска (record)
    │   └── BenchmarkReport.cs          — полный отчёт (record)
    ├── Runner/
    │   └── TestRunner.cs               — Build() + Run() через Process
    ├── Migrations/
    │   └── MigrationManager.cs         — AddFakeMigrations() + RemoveFakeMigrations()
    └── Report/
        ├── ReportGenerator.cs          — JSON → HTML
        └── report-template.html        — Chart.js шаблон с /*INJECT_JSON*/

benchmark-results/                      — создаётся при запуске (gitignored)
    ├── report.html
    └── results.json
```

---

## Task 1: Project scaffold

**Files:**
- Create: `tools/BenchmarkRunner/BenchmarkRunner.csproj`
- Create: `tools/BenchmarkRunner/Program.cs`
- Modify: `.gitignore`

- [ ] **Создать директорию и .csproj**

```xml
<!-- tools/BenchmarkRunner/BenchmarkRunner.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>BenchmarkRunner</RootNamespace>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <None Update="Report\report-template.html">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </None>
  </ItemGroup>
</Project>
```

- [ ] **Создать временный Program.cs**

```csharp
// tools/BenchmarkRunner/Program.cs
Console.WriteLine("BenchmarkRunner OK");
```

- [ ] **Добавить benchmark-results/ в .gitignore**

Добавить в конец `.gitignore` в корне репо:
```
benchmark-results/
```

- [ ] **Проверить что проект собирается**

```bash
dotnet run --project tools/BenchmarkRunner
```

Ожидаемый вывод: `BenchmarkRunner OK`

- [ ] **Commit**

```bash
git add tools/BenchmarkRunner/ .gitignore
git commit -m "feat: добавить проект BenchmarkRunner (scaffold)"
```

---

## Task 2: Models

**Files:**
- Create: `tools/BenchmarkRunner/Models/BenchmarkScenario.cs`
- Create: `tools/BenchmarkRunner/Models/BenchmarkResult.cs`
- Create: `tools/BenchmarkRunner/Models/BenchmarkReport.cs`

- [ ] **Создать BenchmarkScenario.cs**

```csharp
// tools/BenchmarkRunner/Models/BenchmarkScenario.cs
namespace BenchmarkRunner.Models;

public record BenchmarkScenario(
    string Approach,           // "IntegreSQL" | "Respawn" | "Testcontainers"
    string ScenarioName,       // "migrations" | "scale" | "parallelism"
    int    MigrationCount,
    int    TestRepeat,
    int    MaxParallelThreads
);
```

- [ ] **Создать BenchmarkResult.cs**

```csharp
// tools/BenchmarkRunner/Models/BenchmarkResult.cs
namespace BenchmarkRunner.Models;

public record BenchmarkResult(
    BenchmarkScenario Scenario,
    double            ElapsedSeconds,
    bool              Success
);
```

- [ ] **Создать BenchmarkReport.cs**

```csharp
// tools/BenchmarkRunner/Models/BenchmarkReport.cs
namespace BenchmarkRunner.Models;

public record BenchmarkReport(
    DateTime                      GeneratedAt,
    string                        MachineName,
    IReadOnlyList<BenchmarkResult> Results
);
```

- [ ] **Проверить сборку**

```bash
dotnet build tools/BenchmarkRunner --nologo -v q
```

Ожидаемый вывод: `Build succeeded.`

- [ ] **Commit**

```bash
git add tools/BenchmarkRunner/Models/
git commit -m "feat: добавить модели BenchmarkScenario, BenchmarkResult, BenchmarkReport"
```

---

## Task 3: TestRunner

**Files:**
- Create: `tools/BenchmarkRunner/Runner/TestRunner.cs`

- [ ] **Создать TestRunner.cs**

```csharp
// tools/BenchmarkRunner/Runner/TestRunner.cs
using System.Diagnostics;
using BenchmarkRunner.Models;

namespace BenchmarkRunner.Runner;

class TestRunner
{
    private readonly string _repoRoot;

    public TestRunner(string repoRoot) => _repoRoot = repoRoot;

    /// <summary>Собирает тестовый проект. Вызывается перед первым Run и после изменения миграций.</summary>
    public void Build()
    {
        Console.WriteLine("\n[BUILD] tests/FastIntegrationTests.Tests...");
        RunProcess("dotnet", "build tests/FastIntegrationTests.Tests --nologo -v minimal");
        Console.WriteLine("[BUILD] OK");
    }

    /// <summary>Запускает dotnet test для одного сценария и возвращает результат с временем.</summary>
    public BenchmarkResult Run(BenchmarkScenario scenario)
    {
        Console.WriteLine(
            $"\n[RUN] {scenario.Approach,-15} | scenario={scenario.ScenarioName,-12} " +
            $"| migrations={scenario.MigrationCount,3} | repeat={scenario.TestRepeat,2} | threads={scenario.MaxParallelThreads}");

        var filter = $"FullyQualifiedName~Tests.{scenario.Approach}";
        var args   = $"test tests/FastIntegrationTests.Tests" +
                     $" --filter \"{filter}\"" +
                     $" --no-build" +
                     $" -- xUnit.MaxParallelThreads={scenario.MaxParallelThreads}";

        var psi = new ProcessStartInfo("dotnet", args)
        {
            WorkingDirectory = _repoRoot,
            UseShellExecute  = false,
        };
        psi.Environment["TEST_REPEAT"] = scenario.TestRepeat.ToString();

        var sw = Stopwatch.StartNew();
        using var process = Process.Start(psi)!;
        process.WaitForExit();
        sw.Stop();

        var success = process.ExitCode == 0;
        Console.WriteLine($"[RUN] {scenario.Approach}: {sw.Elapsed.TotalSeconds:F2}s | {(success ? "OK" : "FAIL")}");

        return new BenchmarkResult(scenario, sw.Elapsed.TotalSeconds, success);
    }

    private void RunProcess(string filename, string args)
    {
        var psi = new ProcessStartInfo(filename, args)
        {
            WorkingDirectory = _repoRoot,
            UseShellExecute  = false,
        };
        using var process = Process.Start(psi)!;
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new Exception($"Process exited with code {process.ExitCode}: {filename} {args}");
    }
}
```

- [ ] **Обновить Program.cs для дымового теста TestRunner**

```csharp
// tools/BenchmarkRunner/Program.cs
using BenchmarkRunner.Models;
using BenchmarkRunner.Runner;

var repoRoot = FindRepoRoot();
var runner   = new TestRunner(repoRoot);

runner.Build();

var result = runner.Run(new BenchmarkScenario("IntegreSQL", "smoke", 16, 1, 4));
Console.WriteLine($"\nSmoke test: {result.ElapsedSeconds:F2}s | success={result.Success}");

static string FindRepoRoot()
{
    var dir = Directory.GetCurrentDirectory();
    while (dir is not null)
    {
        if (File.Exists(Path.Combine(dir, "FastIntegrationTests.slnx")))
            return dir;
        dir = Directory.GetParent(dir)?.FullName;
    }
    throw new Exception("Repo root not found (FastIntegrationTests.slnx missing)");
}
```

- [ ] **Запустить дымовой тест (Docker должен быть запущен)**

```bash
dotnet run --project tools/BenchmarkRunner
```

Ожидаемый вывод: сборка, затем запуск IntegreSQL тестов, затем строка `Smoke test: XX.XXs | success=True`

- [ ] **Commit**

```bash
git add tools/BenchmarkRunner/Runner/ tools/BenchmarkRunner/Program.cs
git commit -m "feat: добавить TestRunner — запуск dotnet test через Process с замером времени"
```

---

## Task 4: MigrationManager

**Files:**
- Create: `tools/BenchmarkRunner/Migrations/MigrationManager.cs`

MigrationManager пишет `.cs` файлы напрямую в папку миграций Infrastructure — без `dotnet ef migrations add`. Фейковые миграции используют только `migrationBuilder.Sql()` и не затрагивают EF snapshot.

Timestamp `29990101NNNNNN` гарантирует что фейковые миграции идут после всех реальных.

- [ ] **Создать MigrationManager.cs**

```csharp
// tools/BenchmarkRunner/Migrations/MigrationManager.cs
namespace BenchmarkRunner.Migrations;

class MigrationManager
{
    private readonly string _migrationsPath;
    private const string FakePrefix = "Benchmark_Fake_";

    public MigrationManager(string repoRoot)
    {
        _migrationsPath = Path.Combine(
            repoRoot, "src", "FastIntegrationTests.Infrastructure", "Migrations");
    }

    public void AddFakeMigrations(int count)
    {
        if (!Directory.Exists(_migrationsPath))
            throw new DirectoryNotFoundException(
                $"Migrations directory not found: {_migrationsPath}");

        Console.WriteLine($"\n[MIGRATIONS] Adding {count} fake migrations...");
        for (var i = 1; i <= count; i++)
        {
            var name        = $"{FakePrefix}{i:D3}";
            var timestamp   = $"29990101{i:D6}";
            var migrationId = $"{timestamp}_{name}";
            var path        = Path.Combine(_migrationsPath, $"{migrationId}.cs");
            File.WriteAllText(path, GenerateContent(name, migrationId, i));
        }
        Console.WriteLine($"[MIGRATIONS] Added {count} fake migrations");
    }

    public void RemoveFakeMigrations()
    {
        Console.WriteLine("\n[MIGRATIONS] Removing fake migrations...");
        var files = Directory.GetFiles(_migrationsPath, $"*{FakePrefix}*");
        foreach (var f in files) File.Delete(f);
        Console.WriteLine($"[MIGRATIONS] Removed {files.Length} files");
    }

    private static string GenerateContent(string name, string migrationId, int index)
    {
        var isOdd   = index % 2 == 1;
        var upSql   = isOdd ? OddUpSql(index)   : EvenUpSql(index);
        var downSql = isOdd ? OddDownSql(index)  : EvenDownSql(index);

        // Шаблон генерирует валидный C# файл миграции.
        // В выходном файле используются @"..." вербатим-строки.
        // Двойные кавычки PostgreSQL (""Products"") корректно экранированы.
        return
$@"using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FastIntegrationTests.Infrastructure.Migrations
{{
    [Migration(""{migrationId}"")]
    public class {name} : Migration
    {{
        protected override void Up(MigrationBuilder migrationBuilder)
        {{
            migrationBuilder.Sql(@""{upSql}"");
        }}

        protected override void Down(MigrationBuilder migrationBuilder)
        {{
            migrationBuilder.Sql(@""{downSql}"");
        }}
    }}
}}
";
    }

    // Нечётные: создать справочную таблицу + INSERT 300 строк (~10–20 мс)
    private static string OddUpSql(int i) =>
$@"
CREATE TABLE benchmark_ref_{i:D3} (
    id         SERIAL       PRIMARY KEY,
    code       VARCHAR(20)  NOT NULL,
    name       VARCHAR(100) NOT NULL,
    created_at TIMESTAMP    NOT NULL DEFAULT NOW()
);
INSERT INTO benchmark_ref_{i:D3} (code, name)
SELECT 'CODE_' || gs, 'Reference value number ' || gs
FROM generate_series(1, 300) gs;
";

    private static string OddDownSql(int i) =>
        $"DROP TABLE IF EXISTS benchmark_ref_{i:D3};";

    // Чётные: ADD COLUMN + UPDATE + SET NOT NULL (~5–15 мс)
    // ""Products"" — экранированные двойные кавычки для PostgreSQL идентификатора
    private static string EvenUpSql(int i) =>
$@"
ALTER TABLE ""Products"" ADD COLUMN benchmark_col_{i:D3} TEXT NULL;
UPDATE ""Products"" SET benchmark_col_{i:D3} = 'default_value';
ALTER TABLE ""Products"" ALTER COLUMN benchmark_col_{i:D3} SET NOT NULL;
ALTER TABLE ""Products"" ALTER COLUMN benchmark_col_{i:D3} SET DEFAULT 'default_value';
";

    private static string EvenDownSql(int i) =>
        $@"ALTER TABLE ""Products"" DROP COLUMN IF EXISTS benchmark_col_{i:D3};";
}
```

- [ ] **Обновить Program.cs для проверки MigrationManager**

```csharp
// tools/BenchmarkRunner/Program.cs
using BenchmarkRunner.Migrations;
using BenchmarkRunner.Models;
using BenchmarkRunner.Runner;

var repoRoot         = FindRepoRoot();
var runner           = new TestRunner(repoRoot);
var migrationManager = new MigrationManager(repoRoot);

// Проверяем: добавить 3 фейковые миграции, убедиться что файлы создались,
// потом удалить
migrationManager.AddFakeMigrations(3);

var migrationsDir = Path.Combine(repoRoot, "src", "FastIntegrationTests.Infrastructure", "Migrations");
var fakeFiles     = Directory.GetFiles(migrationsDir, "*Benchmark_Fake*");
Console.WriteLine($"\nFake files created: {fakeFiles.Length}");
foreach (var f in fakeFiles) Console.WriteLine($"  {Path.GetFileName(f)}");

// Проверяем что сгенерированный C# файл синтаксически корректен
runner.Build();

migrationManager.RemoveFakeMigrations();
Console.WriteLine("\nFake migrations removed OK");

static string FindRepoRoot()
{
    var dir = Directory.GetCurrentDirectory();
    while (dir is not null)
    {
        if (File.Exists(Path.Combine(dir, "FastIntegrationTests.slnx")))
            return dir;
        dir = Directory.GetParent(dir)?.FullName;
    }
    throw new Exception("Repo root not found (FastIntegrationTests.slnx missing)");
}
```

- [ ] **Запустить проверку**

```bash
dotnet run --project tools/BenchmarkRunner
```

Ожидаемый вывод:
```
[MIGRATIONS] Adding 3 fake migrations...
[MIGRATIONS] Added 3 fake migrations

Fake files created: 3
  29990101000001_Benchmark_Fake_001.cs
  29990101000002_Benchmark_Fake_002.cs
  29990101000003_Benchmark_Fake_003.cs
[BUILD] tests/FastIntegrationTests.Tests...
[BUILD] OK
[MIGRATIONS] Removing fake migrations...
[MIGRATIONS] Removed 3 files
Fake migrations removed OK
```

Если сборка падает — открыть один из сгенерированных файлов и проверить синтаксис вручную.

- [ ] **Commit**

```bash
git add tools/BenchmarkRunner/Migrations/ tools/BenchmarkRunner/Program.cs
git commit -m "feat: добавить MigrationManager — генерация и удаление фейковых .cs миграций"
```

---

## Task 5: ReportGenerator + HTML template

**Files:**
- Create: `tools/BenchmarkRunner/Report/ReportGenerator.cs`
- Create: `tools/BenchmarkRunner/Report/report-template.html`

- [ ] **Создать ReportGenerator.cs**

```csharp
// tools/BenchmarkRunner/Report/ReportGenerator.cs
using System.Text.Json;
using BenchmarkRunner.Models;

namespace BenchmarkRunner.Report;

class ReportGenerator
{
    private readonly string _templatePath;
    private readonly string _outputDir;

    public ReportGenerator(string repoRoot)
    {
        _templatePath = Path.Combine(AppContext.BaseDirectory, "Report", "report-template.html");
        _outputDir    = Path.Combine(repoRoot, "benchmark-results");
    }

    public void Generate(BenchmarkReport report)
    {
        Directory.CreateDirectory(_outputDir);

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented        = true,
        };

        var json     = JsonSerializer.Serialize(report, options);
        var jsonPath = Path.Combine(_outputDir, "results.json");
        File.WriteAllText(jsonPath, json);
        Console.WriteLine($"\n[REPORT] results.json saved");

        var template = File.ReadAllText(_templatePath);
        var html     = template.Replace("/*INJECT_JSON*/", json);
        var htmlPath = Path.Combine(_outputDir, "report.html");
        File.WriteAllText(htmlPath, html);
        Console.WriteLine($"[REPORT] report.html saved: {htmlPath}");
    }
}
```

- [ ] **Создать report-template.html**

```html
<!DOCTYPE html>
<html lang="ru">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>Integration Test Benchmark</title>
  <script src="https://cdn.jsdelivr.net/npm/chart.js@4"></script>
  <style>
    *, *::before, *::after { box-sizing: border-box; }
    body {
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
      max-width: 1100px; margin: 0 auto; padding: 24px;
      background: #f9fafb; color: #111;
    }
    h1 { font-size: 1.5rem; margin-bottom: 4px; }
    .meta { color: #6b7280; font-size: 0.82rem; margin-bottom: 32px; }
    .card {
      background: #fff; border-radius: 8px; padding: 24px;
      margin-bottom: 32px; box-shadow: 0 1px 3px rgba(0,0,0,.08);
    }
    h2 { font-size: 1.05rem; margin: 0 0 4px; }
    .subtitle { color: #6b7280; font-size: 0.8rem; margin: 0 0 20px; }
    canvas { max-height: 350px; }
  </style>
</head>
<body>
  <script>const BENCHMARK_DATA = /*INJECT_JSON*/;</script>

  <h1>Integration Test Benchmark</h1>
  <p class="meta" id="meta"></p>

  <div class="card">
    <h2>Сценарий 1 — Влияние числа миграций</h2>
    <p class="subtitle">TEST_REPEAT=10, потоков=4 &nbsp;|&nbsp; Ось X: количество миграций в проекте</p>
    <canvas id="chart-migrations"></canvas>
  </div>

  <div class="card">
    <h2>Сценарий 2 — Масштаб числа тестов</h2>
    <p class="subtitle">16 миграций, потоков=4 &nbsp;|&nbsp; Ось X: TEST_REPEAT (число повторов каждого теста)</p>
    <canvas id="chart-scale"></canvas>
  </div>

  <div class="card">
    <h2>Сценарий 3 — Параллелизм</h2>
    <p class="subtitle">16 миграций, TEST_REPEAT=20 &nbsp;|&nbsp; Ось X: MaxParallelThreads</p>
    <canvas id="chart-parallelism"></canvas>
  </div>

  <script>
    const data = BENCHMARK_DATA;

    document.getElementById('meta').textContent =
      'Сгенерировано: ' + new Date(data.generatedAt).toLocaleString('ru') +
      '  |  Машина: ' + data.machineName;

    const COLORS = {
      IntegreSQL:     { border: 'rgb(59,130,246)',  bg: 'rgba(59,130,246,0.12)'  },
      Respawn:        { border: 'rgb(34,197,94)',   bg: 'rgba(34,197,94,0.12)'   },
      Testcontainers: { border: 'rgb(239,68,68)',   bg: 'rgba(239,68,68,0.12)'   },
    };

    const APPROACHES = ['IntegreSQL', 'Respawn', 'Testcontainers'];

    function buildChart(canvasId, scenarioName, xField, xLabel) {
      const rows    = data.results.filter(r => r.scenario.scenarioName === scenarioName);
      const xValues = [...new Set(rows.map(r => r.scenario[xField]))].sort((a, b) => a - b);

      const datasets = APPROACHES.map(approach => ({
        label:           approach,
        data:            xValues.map(x => {
          const row = rows.find(r => r.scenario.approach === approach && r.scenario[xField] === x);
          return row ? +row.elapsedSeconds.toFixed(2) : null;
        }),
        borderColor:     COLORS[approach].border,
        backgroundColor: COLORS[approach].bg,
        borderWidth:     2,
        pointRadius:     5,
        tension:         0.15,
        fill:            false,
      }));

      new Chart(document.getElementById(canvasId), {
        type: 'line',
        data: { labels: xValues, datasets },
        options: {
          responsive: true,
          interaction: { mode: 'index', intersect: false },
          plugins: {
            legend: { position: 'top' },
            tooltip: {
              callbacks: { label: ctx => ctx.dataset.label + ': ' + ctx.parsed.y + ' с' }
            }
          },
          scales: {
            x: { title: { display: true, text: xLabel } },
            y: { title: { display: true, text: 'Время (секунды)' }, beginAtZero: true }
          }
        }
      });
    }

    buildChart('chart-migrations', 'migrations', 'migrationCount',     'Количество миграций');
    buildChart('chart-scale',      'scale',      'testRepeat',          'TEST_REPEAT');
    buildChart('chart-parallelism','parallelism', 'maxParallelThreads', 'MaxParallelThreads');
  </script>
</body>
</html>
```

- [ ] **Обновить Program.cs для проверки ReportGenerator**

```csharp
// tools/BenchmarkRunner/Program.cs
using BenchmarkRunner.Migrations;
using BenchmarkRunner.Models;
using BenchmarkRunner.Report;
using BenchmarkRunner.Runner;

var repoRoot      = FindRepoRoot();
var reportGen     = new ReportGenerator(repoRoot);

// Тестовые данные — проверяем что JSON инлайнится и HTML открывается
var fakeReport = new BenchmarkReport(
    DateTime.UtcNow,
    Environment.MachineName,
    new List<BenchmarkResult>
    {
        new(new BenchmarkScenario("IntegreSQL",     "migrations", 16, 10, 4), 12.3, true),
        new(new BenchmarkScenario("Respawn",        "migrations", 16, 10, 4),  9.1, true),
        new(new BenchmarkScenario("Testcontainers", "migrations", 16, 10, 4), 45.6, true),
        new(new BenchmarkScenario("IntegreSQL",     "migrations", 66, 10, 4), 13.1, true),
        new(new BenchmarkScenario("Respawn",        "migrations", 66, 10, 4), 11.2, true),
        new(new BenchmarkScenario("Testcontainers", "migrations", 66, 10, 4),145.0, true),
        new(new BenchmarkScenario("IntegreSQL",     "migrations",116, 10, 4), 13.5, true),
        new(new BenchmarkScenario("Respawn",        "migrations",116, 10, 4), 13.8, true),
        new(new BenchmarkScenario("Testcontainers", "migrations",116, 10, 4),245.0, true),
        new(new BenchmarkScenario("IntegreSQL",     "scale",      16,  1, 4),  2.1, true),
        new(new BenchmarkScenario("Respawn",        "scale",      16,  1, 4),  1.8, true),
        new(new BenchmarkScenario("Testcontainers", "scale",      16,  1, 4),  4.5, true),
        new(new BenchmarkScenario("IntegreSQL",     "scale",      16, 20, 4), 18.0, true),
        new(new BenchmarkScenario("Respawn",        "scale",      16, 20, 4), 15.0, true),
        new(new BenchmarkScenario("Testcontainers", "scale",      16, 20, 4), 88.0, true),
        new(new BenchmarkScenario("IntegreSQL",     "parallelism",16, 20, 1), 55.0, true),
        new(new BenchmarkScenario("Respawn",        "parallelism",16, 20, 1), 60.0, true),
        new(new BenchmarkScenario("Testcontainers", "parallelism",16, 20, 1),180.0, true),
        new(new BenchmarkScenario("IntegreSQL",     "parallelism",16, 20, 8), 12.0, true),
        new(new BenchmarkScenario("Respawn",        "parallelism",16, 20, 8), 55.0, true),
        new(new BenchmarkScenario("Testcontainers", "parallelism",16, 20, 8), 80.0, true),
    });

reportGen.Generate(fakeReport);
Console.WriteLine("\nOpen: benchmark-results/report.html");

static string FindRepoRoot()
{
    var dir = Directory.GetCurrentDirectory();
    while (dir is not null)
    {
        if (File.Exists(Path.Combine(dir, "FastIntegrationTests.slnx")))
            return dir;
        dir = Directory.GetParent(dir)?.FullName;
    }
    throw new Exception("Repo root not found");
}
```

- [ ] **Запустить и проверить report.html в браузере**

```bash
dotnet run --project tools/BenchmarkRunner
start benchmark-results/report.html
```

Ожидаемый результат: браузер открывает страницу с тремя линейными графиками. Все три подхода видны на каждом графике. Легенда и подписи осей на русском.

- [ ] **Commit**

```bash
git add tools/BenchmarkRunner/Report/ tools/BenchmarkRunner/Program.cs
git commit -m "feat: добавить ReportGenerator и HTML шаблон с Chart.js"
```

---

## Task 6: Program.cs — полная оркестрация

**Files:**
- Modify: `tools/BenchmarkRunner/Program.cs`

Заменяем тестовый Program.cs на финальный с тремя реальными сценариями.

- [ ] **Написать финальный Program.cs**

```csharp
// tools/BenchmarkRunner/Program.cs
using BenchmarkRunner.Migrations;
using BenchmarkRunner.Models;
using BenchmarkRunner.Report;
using BenchmarkRunner.Runner;

var repoRoot         = FindRepoRoot();
var runner           = new TestRunner(repoRoot);
var migrationManager = new MigrationManager(repoRoot);
var results          = new List<BenchmarkResult>();

const int BaseMigrations = 16;
var approaches = new[] { "IntegreSQL", "Respawn", "Testcontainers" };

Console.WriteLine("=== Integration Test Benchmark Runner ===");
Console.WriteLine($"Repo:    {repoRoot}");
Console.WriteLine($"Machine: {Environment.MachineName}");
Console.WriteLine($"Time:    {DateTime.Now:yyyy-MM-dd HH:mm}");
Console.WriteLine("\nDocker must be running. Full run takes 15-30 min.");
Console.WriteLine("Press Enter to start, Ctrl+C to cancel...");
Console.ReadLine();

// Первичная сборка
runner.Build();

// ─── Сценарий 1: влияние числа миграций ────────────────────────────────────
Console.WriteLine("\n═══ Scenario 1: Migration Count Impact ═══");
foreach (var migrationCount in new[] { 16, 66, 116 })
{
    var fakesToAdd = migrationCount - BaseMigrations;
    if (fakesToAdd > 0)
    {
        try
        {
            migrationManager.AddFakeMigrations(fakesToAdd);
            runner.Build();

            foreach (var approach in approaches)
                results.Add(runner.Run(
                    new BenchmarkScenario(approach, "migrations", migrationCount, testRepeat: 10, maxParallelThreads: 4)));
        }
        finally
        {
            migrationManager.RemoveFakeMigrations();
            runner.Build(); // вернуть чистое состояние
        }
    }
    else
    {
        foreach (var approach in approaches)
            results.Add(runner.Run(
                new BenchmarkScenario(approach, "migrations", migrationCount, testRepeat: 10, maxParallelThreads: 4)));
    }
}

// ─── Сценарий 2: масштаб числа тестов ──────────────────────────────────────
Console.WriteLine("\n═══ Scenario 2: Test Count Scaling ═══");
foreach (var repeat in new[] { 1, 5, 10, 20, 50 })
    foreach (var approach in approaches)
        results.Add(runner.Run(
            new BenchmarkScenario(approach, "scale", BaseMigrations, testRepeat: repeat, maxParallelThreads: 4)));

// ─── Сценарий 3: параллелизм ────────────────────────────────────────────────
Console.WriteLine("\n═══ Scenario 3: Parallelism ═══");
foreach (var threads in new[] { 1, 2, 4, 8 })
    foreach (var approach in approaches)
        results.Add(runner.Run(
            new BenchmarkScenario(approach, "parallelism", BaseMigrations, testRepeat: 20, maxParallelThreads: threads)));

// ─── Генерация отчёта ───────────────────────────────────────────────────────
var report = new BenchmarkReport(DateTime.UtcNow, Environment.MachineName, results);
new ReportGenerator(repoRoot).Generate(report);

Console.WriteLine("\n=== Done! ===");
Console.WriteLine("Open: benchmark-results/report.html");

// ─── Вспомогательная функция ────────────────────────────────────────────────
static string FindRepoRoot()
{
    var dir = Directory.GetCurrentDirectory();
    while (dir is not null)
    {
        if (File.Exists(Path.Combine(dir, "FastIntegrationTests.slnx")))
            return dir;
        dir = Directory.GetParent(dir)?.FullName;
    }
    throw new Exception("Repo root not found (FastIntegrationTests.slnx missing)");
}
```

- [ ] **Проверить что проект компилируется**

```bash
dotnet build tools/BenchmarkRunner --nologo -v q
```

Ожидаемый вывод: `Build succeeded.`

- [ ] **Commit**

```bash
git add tools/BenchmarkRunner/Program.cs
git commit -m "feat: финальный Program.cs — три сценария бенчмарка"
```

---

## Task 7: End-to-end запуск

- [ ] **Убедиться что Docker запущен**

```bash
docker info
```

Должен вернуть информацию о Docker daemon без ошибок.

- [ ] **Запустить полный бенчмарк**

```bash
dotnet run --project tools/BenchmarkRunner
```

Нажать Enter. Ожидать 15–30 минут. Следить за выводом — каждый `[RUN]` строка показывает прогресс.

Ожидаемый финальный вывод:
```
[REPORT] results.json saved
[REPORT] report.html saved: C:\...\benchmark-results\report.html

=== Done! ===
Open: benchmark-results/report.html
```

- [ ] **Открыть отчёт и проверить графики**

```bash
start benchmark-results/report.html
```

Проверить:
- Три графика отображаются
- График 1 (миграции): линия Testcontainers резко растёт, IntegreSQL почти горизонтальна
- График 2 (scale): Testcontainers растёт быстрее остальных
- График 3 (параллелизм): IntegreSQL снижается с ростом потоков, Respawn — почти горизонталь
- Тултипы показывают время в секундах при наведении
- Шапка содержит дату и имя машины

- [ ] **Проверить results.json**

```bash
cat benchmark-results/results.json | head -40
```

Должен содержать корректный JSON с полем `results` — массивом из 9 + 15 + 12 = 36 объектов (сценарий 1: 3 точки × 3 подхода; сценарий 2: 5 точек × 3; сценарий 3: 4 точки × 3).

- [ ] **Убедиться что фейковые миграции удалены**

```bash
ls src/FastIntegrationTests.Infrastructure/Migrations/*Benchmark* 2>/dev/null && echo "FOUND - ERROR" || echo "Clean - OK"
```

Ожидаемый вывод: `Clean - OK`

- [ ] **Commit итогового состояния**

```bash
git add tools/ .gitignore
git commit -m "feat: BenchmarkRunner — полный рабочий бенчмарк с HTML отчётом"
```

---

## Итоговая структура

```
tools/BenchmarkRunner/
├── BenchmarkRunner.csproj
├── Program.cs
├── Models/
│   ├── BenchmarkScenario.cs
│   ├── BenchmarkResult.cs
│   └── BenchmarkReport.cs
├── Runner/
│   └── TestRunner.cs
├── Migrations/
│   └── MigrationManager.cs
└── Report/
    ├── ReportGenerator.cs
    └── report-template.html

benchmark-results/          ← gitignored, создаётся при запуске
├── report.html
└── results.json
```

**Запуск:**
```bash
dotnet run --project tools/BenchmarkRunner
start benchmark-results/report.html
```
