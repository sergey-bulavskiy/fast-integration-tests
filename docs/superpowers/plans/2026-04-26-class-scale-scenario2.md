# Class Scale Scenario 2 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Заменить TEST_REPEAT-механизм на масштабирование числа тест-классов, чтобы Сценарий 2 честно воспроизводил per-class overhead Respawn и Testcontainers.

**Architecture:** Новый `ClassScaleManager` генерирует пустые подклассы для каждого тест-класса в трёх тест-проектах, что даёт отдельный `IClassFixture` на каждый подкласс. TEST_REPEAT удаляется из инфраструктуры тестов — все `[Theory]+[MemberData]` становятся `[Fact]`. Все три сценария BenchmarkRunner переходят на `ClassScale` вместо `TestRepeat`.

**Tech Stack:** C# / .NET 8, xUnit, PowerShell (для bulk-edit), Chart.js (HTML-отчёт)

---

## Файловая карта

| Файл | Действие |
|---|---|
| `tools/BenchmarkRunner/Scale/ClassScaleManager.cs` | создать |
| `tools/BenchmarkRunner/Models/BenchmarkScenario.cs` | изменить: `TestRepeat` → `ClassScale` |
| `tools/BenchmarkRunner/Runner/TestRunner.cs` | изменить: убрать TEST_REPEAT env, обновить лог |
| `tools/BenchmarkRunner/Program.cs` | изменить: все сценарии + `--repeat`→`--scale` |
| `tools/BenchmarkRunner/Report/report-template.html` | изменить: `testRepeat` → `classScale` (4 места) |
| `tests/FastIntegrationTests.Tests.Shared/Infrastructure/Base/TestRepeat.cs` | удалить |
| 84 тест-класса (28 × 3 проекта) | `[Theory]+MemberData+int _` → `[Fact]` |
| `README.md` | обновить CLI-аргументы и таблицу сценариев |
| `CLAUDE.md` | обновить CLI-аргументы и описание Сценария 2 |
| `run-integresql.ps1`, `run-respawn.ps1`, `run-testcontainers.ps1` | убрать `$env:TEST_REPEAT` и `-Repeat` |
| `docs/benchmark-issues/03-scenario2-wrong-migration-count.md` | пометить исторический |
| `docs/benchmark-issues/05-test-repeat-vs-real-classes.md` | добавить раздел «Решение» |

---

## Task 1: Удалить TestRepeat из тест-инфраструктуры

**Files:**
- Delete: `tests/FastIntegrationTests.Tests.Shared/Infrastructure/Base/TestRepeat.cs`
- Modify: все `.cs` файлы тестов в `tests/` (84 файла)

- [ ] **Step 1: Bulk-replace через PowerShell**

Запустить из корня репозитория:

```powershell
Get-ChildItem -Path "tests" -Recurse -Filter "*.cs" |
    Where-Object { $_.FullName -notmatch '\\obj\\' -and $_.Name -ne 'GlobalUsings.cs' -and $_.Name -ne 'TestRepeat.cs' } |
    Where-Object { (Get-Content $_.FullName -Raw) -match 'TestRepeat' } |
    ForEach-Object {
        $content = Get-Content $_.FullName -Raw
        # Убрать строку [MemberData(nameof(TestRepeat.Data)...)]
        $content = $content -replace '\r?\n[ \t]*\[MemberData\(nameof\(TestRepeat\.Data\).*?\)\]', ''
        # [Theory] → [Fact]
        $content = $content -replace '\[Theory\]', '[Fact]'
        # (int _) → ()
        $content = $content -replace '\(int _\)', '()'
        [System.IO.File]::WriteAllText($_.FullName, $content, [System.Text.Encoding]::UTF8)
    }
```

- [ ] **Step 2: Удалить TestRepeat.cs**

```powershell
Remove-Item "tests\FastIntegrationTests.Tests.Shared\Infrastructure\Base\TestRepeat.cs"
```

- [ ] **Step 3: Убедиться что проект собирается**

```powershell
dotnet build
```

Ожидаемый результат: `Build succeeded` без ошибок.

- [ ] **Step 4: Убедиться что тесты запускаются**

```powershell
dotnet test tests/FastIntegrationTests.Tests.IntegreSQL --list-tests 2>&1 | Select-String "::" | Measure-Object | Select-Object Count
```

Ожидаемый результат: `Count : 223` (то же что было при TEST_REPEAT=1).

- [ ] **Step 5: Commit**

```powershell
git add -A
git commit -m "refactor: убрать TEST_REPEAT — Theory+MemberData → Fact во всех тест-классах"
```

---

## Task 2: Создать ClassScaleManager

**Files:**
- Create: `tools/BenchmarkRunner/Scale/ClassScaleManager.cs`

- [ ] **Step 1: Создать файл**

`tools/BenchmarkRunner/Scale/ClassScaleManager.cs`:

```csharp
// tools/BenchmarkRunner/Scale/ClassScaleManager.cs
using System.Text;
using System.Text.RegularExpressions;

namespace BenchmarkRunner.Scale;

/// <summary>Генерирует и удаляет файлы подклассов для масштабирования числа тест-классов.</summary>
public class ClassScaleManager
{
    private const string GeneratedFileName = "BenchmarkScaleClasses.cs";

    private readonly string[] _testProjectPaths;

    /// <summary>Инициализирует менеджер с корневой директорией репозитория.</summary>
    public ClassScaleManager(string repoRoot)
    {
        _testProjectPaths =
        [
            Path.Combine(repoRoot, "tests", "FastIntegrationTests.Tests.IntegreSQL"),
            Path.Combine(repoRoot, "tests", "FastIntegrationTests.Tests.Respawn"),
            Path.Combine(repoRoot, "tests", "FastIntegrationTests.Tests.Testcontainers"),
        ];
    }

    /// <summary>
    /// Генерирует <c>(scaleFactor - 1)</c> подклассов для каждого тест-класса в трёх проектах.
    /// Каждый подкласс получает свой <c>IClassFixture</c> — честный per-class overhead.
    /// </summary>
    public void AddScaleClasses(int scaleFactor)
    {
        if (scaleFactor <= 1) return;

        Console.WriteLine($"\n[SCALE] Adding {scaleFactor - 1} extra copies per class (total factor: {scaleFactor})...");
        foreach (var projectPath in _testProjectPaths)
        {
            var classes = DiscoverTestClasses(projectPath);
            var content = GenerateScaleFile(classes, scaleFactor);
            File.WriteAllText(Path.Combine(projectPath, GeneratedFileName), content);
            Console.WriteLine($"[SCALE] {Path.GetFileName(projectPath)}: {classes.Count} classes × {scaleFactor - 1} copies");
        }
    }

    /// <summary>Удаляет сгенерированные файлы из трёх тест-проектов.</summary>
    public void RemoveScaleClasses()
    {
        foreach (var projectPath in _testProjectPaths)
        {
            var path = Path.Combine(projectPath, GeneratedFileName);
            if (!File.Exists(path)) continue;
            File.Delete(path);
            Console.WriteLine($"[SCALE] Removed {Path.GetFileName(projectPath)}/{GeneratedFileName}");
        }
    }

    private static List<TestClassInfo> DiscoverTestClasses(string projectPath)
    {
        var result = new List<TestClassInfo>();
        var sep    = Path.DirectorySeparatorChar;

        var files = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{sep}obj{sep}") &&
                        !f.Contains($"{sep}Infrastructure{sep}") &&
                        !Path.GetFileName(f).Equals("GlobalUsings.cs",       StringComparison.OrdinalIgnoreCase) &&
                        !Path.GetFileName(f).Equals(GeneratedFileName,        StringComparison.OrdinalIgnoreCase));

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            if (!content.Contains("[Fact]") && !content.Contains("[Theory]")) continue;

            var ns          = ExtractNamespace(content);
            var className   = ExtractClassName(content);
            var fixtureType = ExtractFixtureType(content);

            if (ns is null || className is null) continue;
            result.Add(new TestClassInfo(ns, className, fixtureType));
        }

        return result;
    }

    private static string? ExtractNamespace(string content)
    {
        var m = Regex.Match(content, @"^namespace\s+([\w.]+)\s*[;{]", RegexOptions.Multiline);
        return m.Success ? m.Groups[1].Value : null;
    }

    private static string? ExtractClassName(string content)
    {
        var m = Regex.Match(content, @"public\s+class\s+(\w+)");
        return m.Success ? m.Groups[1].Value : null;
    }

    private static string? ExtractFixtureType(string content)
    {
        var m = Regex.Match(content, @"public\s+\w+\((\w+Fixture)\s+fixture\)");
        return m.Success ? m.Groups[1].Value : null;
    }

    private static string GenerateScaleFile(List<TestClassInfo> classes, int scaleFactor)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// BenchmarkScaleClasses.cs — сгенерирован BenchmarkRunner, не редактировать");
        sb.AppendLine("// ReSharper disable All");
        sb.AppendLine("#pragma warning disable");

        foreach (var group in classes.GroupBy(c => c.Namespace))
        {
            sb.AppendLine();
            sb.AppendLine($"namespace {group.Key}");
            sb.AppendLine("{");
            foreach (var cls in group)
            {
                for (var i = 2; i <= scaleFactor; i++)
                {
                    if (cls.FixtureType is null)
                    {
                        sb.AppendLine($"    public class {cls.ClassName}_{i} : {cls.ClassName} {{ }}");
                    }
                    else
                    {
                        sb.AppendLine($"    public class {cls.ClassName}_{i} : {cls.ClassName}");
                        sb.AppendLine("    {");
                        sb.AppendLine($"        public {cls.ClassName}_{i}({cls.FixtureType} fixture) : base(fixture) {{ }}");
                        sb.AppendLine("    }");
                    }
                }
            }
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    private record TestClassInfo(string Namespace, string ClassName, string? FixtureType);
}
```

- [ ] **Step 2: Убедиться что BenchmarkRunner собирается**

```powershell
dotnet build tools/BenchmarkRunner
```

Ожидаемый результат: `Build succeeded`.

- [ ] **Step 3: Проверить генерацию файлов**

Добавить временно в `Program.cs` перед `Console.ReadLine()`:

```csharp
var testScaleManager = new ClassScaleManager(repoRoot);
testScaleManager.RemoveScaleClasses();
testScaleManager.AddScaleClasses(3);
Console.WriteLine("Scale files generated — check tests/ directories. Press Enter to clean up...");
Console.ReadLine();
testScaleManager.RemoveScaleClasses();
Environment.Exit(0);
```

Запустить: `dotnet run --project tools/BenchmarkRunner`

Проверить что создался файл `tests/FastIntegrationTests.Tests.IntegreSQL/BenchmarkScaleClasses.cs`, в нём классы вида `ProductServiceCrTests_2`, `ProductServiceCrTests_3`, и аналогичные для Respawn/Testcontainers с конструктором фикстуры.

Нажать Enter — файлы удалятся. Убрать добавленный временный код из `Program.cs`.

- [ ] **Step 4: Проверить компиляцию тестов с scale-файлами**

```powershell
$sm = [System.IO.Directory]::GetCurrentDirectory()
# Генерируем вручную через простой скрипт
dotnet build tools/BenchmarkRunner
# Создадим файлы, проверим сборку тестов, удалим
```

Фактически: вернуть временный код из Step 3 но добавить `runner.Build()` вместо `Console.ReadLine()`:

```csharp
var testScaleManager = new ClassScaleManager(repoRoot);
testScaleManager.RemoveScaleClasses();
testScaleManager.AddScaleClasses(3);
runner.Build();
Console.WriteLine("Build OK — listing tests...");
// запустить dotnet test --list-tests вручную, убедиться что тестов 3×223
testScaleManager.RemoveScaleClasses();
runner.Build();
Environment.Exit(0);
```

Запустить и проверить: `dotnet test tests/FastIntegrationTests.Tests.IntegreSQL --list-tests 2>&1 | Select-String "::" | Measure-Object` показывает 3 × 223 = 669.

Убрать временный код.

- [ ] **Step 5: Commit**

```powershell
git add tools/BenchmarkRunner/Scale/ClassScaleManager.cs
git commit -m "feat: ClassScaleManager — генерация подклассов для масштабирования числа тест-классов"
```

---

## Task 3: Обновить BenchmarkScenario и TestRunner

**Files:**
- Modify: `tools/BenchmarkRunner/Models/BenchmarkScenario.cs`
- Modify: `tools/BenchmarkRunner/Runner/TestRunner.cs`

- [ ] **Step 1: Обновить BenchmarkScenario.cs**

```csharp
// tools/BenchmarkRunner/Models/BenchmarkScenario.cs
namespace BenchmarkRunner.Models;

/// <summary>
/// Описание сценария бенчмарка.
/// </summary>
/// <param name="Approach">Подход: "IntegreSQL" | "Respawn" | "Testcontainers"</param>
/// <param name="ScenarioName">Тип сценария: "migrations" | "scale" | "parallelism"</param>
/// <param name="MigrationCount">Количество миграций</param>
/// <param name="MaxParallelThreads">Максимальное количество параллельных потоков</param>
/// <param name="ClassScale">Множитель числа тест-классов (по умолчанию 1)</param>
public record BenchmarkScenario(
    string Approach,
    string ScenarioName,
    int MigrationCount,
    int MaxParallelThreads,
    int ClassScale = 1
);
```

- [ ] **Step 2: Обновить TestRunner.cs — три места**

Строка 45 — исправить позиционный вызов в `Build()`:

```csharp
var buildScenario = new BenchmarkScenario("build", "build", 0, 0);
```

Строка 85 — убрать передачу `TEST_REPEAT`:

```csharp
var (output, code) = RunCapture("dotnet", args);
```

Строка 109 — заменить `r={s.TestRepeat,2}` на `s={s.ClassScale,2}`:

```csharp
private static string FormatPrefix(string tag, BenchmarkScenario s) =>
    $"{DateTime.Now:HH:mm} {tag} {s.Approach,-15} {s.ScenarioName,-12} m={s.MigrationCount,3} s={s.ClassScale,2} t={s.MaxParallelThreads} ...";
```

Строка 119 — заменить `r={scenario.TestRepeat}` на `s={scenario.ClassScale}`:

```csharp
$"Scenario: {scenario.Approach} / {scenario.ScenarioName} / m={scenario.MigrationCount} s={scenario.ClassScale} t={scenario.MaxParallelThreads}" + Environment.NewLine +
```

- [ ] **Step 3: Убедиться что BenchmarkRunner собирается**

```powershell
dotnet build tools/BenchmarkRunner
```

Ожидаемый результат: `Build succeeded`. Если есть ошибки компиляции — `Program.cs` ещё ссылается на старые поля, исправить в следующем таске.

- [ ] **Step 4: Commit**

```powershell
git add tools/BenchmarkRunner/Models/BenchmarkScenario.cs tools/BenchmarkRunner/Runner/TestRunner.cs
git commit -m "refactor: BenchmarkScenario TestRepeat→ClassScale, убрать TEST_REPEAT env из TestRunner"
```

---

## Task 4: Обновить Program.cs

**Files:**
- Modify: `tools/BenchmarkRunner/Program.cs`

- [ ] **Step 1: Написать новый Program.cs полностью**

```csharp
// tools/BenchmarkRunner/Program.cs
using BenchmarkRunner.Migrations;
using BenchmarkRunner.Models;
using BenchmarkRunner.Report;
using BenchmarkRunner.Runner;
using BenchmarkRunner.Scale;

// ─── Аргументы командной строки ────────────────────────────────────────────
int defaultThreads     = 8;  // для сценариев, где потоки не варьируются (1 и 2)
int defaultClassScale  = 12; // для сценариев, где масштаб не варьируется (1 и 3)
int timeoutMinutes     = 50; // таймаут одного прогона dotnet test

// хардкод — обновить при добавлении/удалении тест-методов в тест-проектах
// проверить: dotnet test tests/FastIntegrationTests.Tests.IntegreSQL --list-tests 2>&1 | grep "FastIntegrationTests.Tests.IntegreSQL" | wc -l
const int BaseTestCount = 223;

for (var i = 0; i < args.Length - 1; i++)
{
    if (args[i] is "--scale" or "-s" && int.TryParse(args[i + 1], out var s) && s > 0)
        defaultClassScale = s;
    if (args[i] is "--threads" or "-t" && int.TryParse(args[i + 1], out var t) && t > 0)
        defaultThreads = t;
    if (args[i] is "--timeout" && int.TryParse(args[i + 1], out var to) && to > 0)
        timeoutMinutes = to;
}

var repoRoot         = FindRepoRoot();
var runner           = new TestRunner(repoRoot, TimeSpan.FromMinutes(timeoutMinutes));
var migrationManager = new MigrationManager(repoRoot);
var classScaleManager = new ClassScaleManager(repoRoot);
var reportGenerator  = new ReportGenerator(repoRoot);
var results          = new List<BenchmarkResult>();

const int BaseMigrations   = 17;
var approaches             = new[] { "IntegreSQL", "Respawn", "Testcontainers" };
var migrationCounts        = new[] { 17, 42, 67, 92, 117 };
var classScaleFactors      = new[] { 1, 5, 10, 20, 50 };
var parallelismThreads     = new[] { 1, 2, 4, 8 };
runner.SetTotalRuns((migrationCounts.Length + classScaleFactors.Length + parallelismThreads.Length) * approaches.Length);

Console.WriteLine("=== Integration Test Benchmark Runner ===");
Console.WriteLine($"Repo:    {repoRoot}");
Console.WriteLine($"Machine: {Environment.MachineName}");
Console.WriteLine($"Time:    {DateTime.Now:yyyy-MM-dd HH:mm}");
Console.WriteLine($"Config:  threads={defaultThreads}, scale={defaultClassScale}, timeout={timeoutMinutes}m");
Console.WriteLine("\nDocker must be running. Full run takes ~1-2 hours.");
Console.WriteLine("Press Enter to start, Ctrl+C to cancel...");
Console.ReadLine();

// Убрать фейковые миграции и scale-классы, которые могли остаться от прерванного прогона
migrationManager.RemoveFakeMigrations();
classScaleManager.RemoveScaleClasses();

// Первичная сборка
runner.Build();

try
{
    // ─── Warmup ─────────────────────────────────────────────────────────────────
    Console.WriteLine($"\n{DateTime.Now:HH:mm} ═══ Warmup (не входит в отчёт) ═══");
    foreach (var approach in approaches)
    {
        var warmup = runner.Warmup(new BenchmarkScenario(approach, "warmup", BaseMigrations, defaultThreads));
        if (!warmup.Success)
            throw new BenchmarkAbortedException();
    }

    // ─── Сценарий 1: влияние числа миграций ────────────────────────────────────
    Console.WriteLine($"\n{DateTime.Now:HH:mm} ═══ Scenario 1: Migration Count Impact ═══");
    classScaleManager.AddScaleClasses(defaultClassScale);
    runner.Build();
    try
    {
        foreach (var migrationCount in migrationCounts)
        {
            var fakesToAdd = migrationCount - BaseMigrations;
            try
            {
                if (fakesToAdd > 0)
                {
                    migrationManager.AddFakeMigrations(fakesToAdd);
                    runner.Build();
                }
                foreach (var approach in approaches)
                    RunOrAbort(new BenchmarkScenario(approach, "migrations", migrationCount, defaultThreads, defaultClassScale));
            }
            finally
            {
                if (fakesToAdd > 0)
                {
                    migrationManager.RemoveFakeMigrations();
                    runner.Build();
                }
            }
        }
    }
    finally
    {
        classScaleManager.RemoveScaleClasses();
        runner.Build();
    }

    // ─── Сценарий 2: масштаб числа тестов ──────────────────────────────────────
    Console.WriteLine($"\n{DateTime.Now:HH:mm} ═══ Scenario 2: Test Count Scaling ═══");
    foreach (var scale in classScaleFactors)
    {
        try
        {
            if (scale > 1)
            {
                classScaleManager.AddScaleClasses(scale);
                runner.Build();
            }
            foreach (var approach in approaches)
                RunOrAbort(new BenchmarkScenario(approach, "scale", BaseMigrations, defaultThreads, scale));
        }
        finally
        {
            if (scale > 1)
            {
                classScaleManager.RemoveScaleClasses();
                runner.Build();
            }
        }
    }

    // ─── Сценарий 3: параллелизм ────────────────────────────────────────────────
    Console.WriteLine($"\n{DateTime.Now:HH:mm} ═══ Scenario 3: Parallelism ═══");
    classScaleManager.AddScaleClasses(defaultClassScale);
    runner.Build();
    try
    {
        foreach (var parallelism in parallelismThreads)
            foreach (var approach in approaches)
                RunOrAbort(new BenchmarkScenario(approach, "parallelism", BaseMigrations, parallelism, defaultClassScale));
    }
    finally
    {
        classScaleManager.RemoveScaleClasses();
        runner.Build();
    }
}
catch (BenchmarkAbortedException)
{
    Console.WriteLine("\n=== BENCHMARK ABORTED ===");
    Console.WriteLine("Fix the failing tests and re-run the benchmark.");
    Environment.Exit(1);
}

// ─── Генерация отчёта ───────────────────────────────────────────────────────
var report   = new BenchmarkReport(DateTime.UtcNow, Environment.MachineName, BaseTestCount, results);
var htmlPath = reportGenerator.Generate(report);

Console.WriteLine("\n=== Done! ===");
Console.WriteLine($"Open: {Path.GetRelativePath(repoRoot, htmlPath)}");

// ─── Вспомогательные функции ────────────────────────────────────────────────
BenchmarkResult RunOrAbort(BenchmarkScenario scenario)
{
    var result = runner.Run(scenario);
    if (!result.Success)
        throw new BenchmarkAbortedException();
    results.Add(result);
    reportGenerator.SaveJson(new BenchmarkReport(DateTime.UtcNow, Environment.MachineName, BaseTestCount, results));
    return result;
}

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

class BenchmarkAbortedException : Exception;
```

- [ ] **Step 2: Убедиться что BenchmarkRunner собирается**

```powershell
dotnet build tools/BenchmarkRunner
```

Ожидаемый результат: `Build succeeded` без ошибок и предупреждений.

- [ ] **Step 3: Commit**

```powershell
git add tools/BenchmarkRunner/Program.cs
git commit -m "feat: Сценарий 2 через ClassScale, Сценарии 1 и 3 — фиксированный масштаб классов"
```

---

## Task 5: Обновить HTML-шаблон

**Files:**
- Modify: `tools/BenchmarkRunner/Report/report-template.html`

- [ ] **Step 1: Заменить testRepeat → classScale в subtitle Сценария 1 (строка ~67)**

Найти:
```js
        `${m1.scenario.testRepeat * BASE_TEST_COUNT} тестов, потоков=${m1.scenario.maxParallelThreads}` +
```
Заменить на:
```js
        `${m1.scenario.classScale * BASE_TEST_COUNT} тестов, потоков=${m1.scenario.maxParallelThreads}` +
```

- [ ] **Step 2: Заменить в subtitle Сценария 3 (строка ~77)**

Найти:
```js
        `${m3.scenario.migrationCount} миграций, ${m3.scenario.testRepeat * BASE_TEST_COUNT} тестов` +
```
Заменить на:
```js
        `${m3.scenario.migrationCount} миграций, ${m3.scenario.classScale * BASE_TEST_COUNT} тестов` +
```

- [ ] **Step 3: Заменить в subtitle timing-чарта (строка ~81)**

Найти:
```js
        `${m1.scenario.testRepeat * BASE_TEST_COUNT} тестов, потоков=${m1.scenario.maxParallelThreads}` +
```
Заменить на:
```js
        `${m1.scenario.classScale * BASE_TEST_COUNT} тестов, потоков=${m1.scenario.maxParallelThreads}` +
```

- [ ] **Step 4: Заменить xField в buildChart для Сценария 2 (строка ~132)**

Найти:
```js
    buildChart('chart-scale',      'scale',      'testRepeat',          'Количество тестов', x => x * BASE_TEST_COUNT);
```
Заменить на:
```js
    buildChart('chart-scale',      'scale',      'classScale',          'Количество тестов', x => x * BASE_TEST_COUNT);
```

- [ ] **Step 5: Убедиться что шаблон не содержит testRepeat**

```powershell
Select-String "testRepeat" tools/BenchmarkRunner/Report/report-template.html
```

Ожидаемый результат: нет совпадений.

- [ ] **Step 6: Commit**

```powershell
git add tools/BenchmarkRunner/Report/report-template.html
git commit -m "fix: HTML-шаблон — testRepeat → classScale (4 места)"
```

---

## Task 6: Обновить документацию и скрипты

**Files:**
- Modify: `README.md`
- Modify: `CLAUDE.md`
- Modify: `run-integresql.ps1`, `run-respawn.ps1`, `run-testcontainers.ps1`
- Modify: `docs/benchmark-issues/05-test-repeat-vs-real-classes.md`
- Modify: `docs/benchmark-issues/03-scenario2-wrong-migration-count.md`

- [ ] **Step 1: Обновить run-*.ps1 — убрать -Repeat и TEST_REPEAT**

Все три скрипта (`run-integresql.ps1`, `run-respawn.ps1`, `run-testcontainers.ps1`) привести к виду:

```powershell
param(
    [int]$Threads = 4
)

$start = Get-Date

Write-Host "IntegreSQL | threads=$Threads"
dotnet test tests/FastIntegrationTests.Tests.IntegreSQL `
    -- xUnit.MaxParallelThreads=$Threads

$elapsed = (Get-Date) - $start
Write-Host "`nВремя выполнения: $($elapsed.ToString('mm\:ss\.fff'))"
```

(Для `run-respawn.ps1` и `run-testcontainers.ps1` — соответствующий проект.)

- [ ] **Step 2: Обновить README.md**

Найти и обновить три блока:

**Блок 1** — пример с TEST_REPEAT (строка ~146):
```markdown
# Все тесты (три подхода вместе)
```
Убрать `TEST_REPEAT=1` из заголовка и примеров команд.

**Блок 2** — аргументы BenchmarkRunner (~строка 192):
```
dotnet run --project tools/BenchmarkRunner -- --threads 4 --repeat 10
```
Заменить на:
```
dotnet run --project tools/BenchmarkRunner -- --threads 4 --scale 12
```

**Блок 3** — таблица аргументов (~строка 202):

| Аргумент | По умолчанию | Применяется в |
|---|---|---|
| `--scale N` / `-s N` | 12 | Сценарии 1 и 3 (масштаб классов) |
| `--threads N` / `-t N` | 8 | Сценарии 1 и 2 |
| `--timeout N` | 50 | Таймаут одного прогона (минуты) |

**Блок 4** — таблица сценариев (~строка 208):

| Сценарий | Фиксируется | Варьируется |
|---|---|---|
| 1 — Влияние числа миграций | `--scale`, `--threads` | 17 / 42 / 67 / 92 / 117 миграций |
| 2 — Масштаб числа тестов | 17 миграций, `--threads` | ClassScale: 1, 5, 10, 20, 50 |
| 3 — Параллелизм | 17 миграций, `--scale` | потоков: 1, 2, 4, 8 |

- [ ] **Step 3: Обновить CLAUDE.md**

Найти блок «С повторами — сравнение производительности» и удалить `TEST_REPEAT=N` примеры командной строки (они больше не актуальны).

В таблице аргументов BenchmarkRunner заменить `--repeat N` / `-r N` → `--scale N` / `-s N`, обновить описание: «масштаб классов для Сценариев 1 и 3».

В описании Сценария 2 заменить «TEST_REPEAT: 1, 5, 10, 20, 50» → «ClassScale: 1, 5, 10, 20, 50».

- [ ] **Step 4: Обновить docs/benchmark-issues/05-test-repeat-vs-real-classes.md**

Добавить в конец файла:

```markdown
## Решение

Реализовано в `docs/superpowers/specs/2026-04-26-class-scale-scenario2-design.md`.

Сценарий 2 теперь варьирует число тест-классов через `ClassScaleManager`:
- `TEST_REPEAT` удалён из тест-инфраструктуры
- Все тест-методы — `[Fact]`, каждый класс запускается с 1 повтором
- `ClassScale ∈ {1, 5, 10, 20, 50}` — множитель числа классов (пик: 50 × 223 ≈ 11 150 тестов)
- Каждый подкласс получает свой `IClassFixture` → честный per-class overhead
```

- [ ] **Step 5: Обновить docs/benchmark-issues/03-scenario2-wrong-migration-count.md**

Добавить вверху файла после заголовка:

```markdown
> **Примечание:** Этот документ описывает историческое состояние кода. `TestRepeat` и связанная инфраструктура удалены — см. `docs/superpowers/specs/2026-04-26-class-scale-scenario2-design.md`.
```

- [ ] **Step 6: Добавить примечание к историческим спекам**

В начало каждого из четырёх файлов (после заголовка) добавить одну строку:

```markdown
> **Примечание:** Механизм `TestRepeat` заменён на `ClassScale` — см. `docs/superpowers/specs/2026-04-26-class-scale-scenario2-design.md`.
```

Файлы:
- `docs/superpowers/specs/2026-04-21-benchmark-design.md`
- `docs/superpowers/specs/2026-04-25-benchmark-timestamps-testcount-design.md`
- `docs/superpowers/specs/2026-04-25-migration-timing-design.md`
- `docs/superpowers/specs/2026-04-25-split-test-projects-design.md`

- [ ] **Step 7: Убедиться что нет оставшихся TEST_REPEAT в .ps1 и README/CLAUDE**

```powershell
Select-String "TEST_REPEAT" run-integresql.ps1, run-respawn.ps1, run-testcontainers.ps1, README.md, CLAUDE.md
```

Ожидаемый результат: нет совпадений.

- [ ] **Step 8: Commit**

```powershell
git add run-integresql.ps1 run-respawn.ps1 run-testcontainers.ps1 README.md CLAUDE.md
git add docs/benchmark-issues/05-test-repeat-vs-real-classes.md docs/benchmark-issues/03-scenario2-wrong-migration-count.md
git add docs/superpowers/specs/2026-04-21-benchmark-design.md docs/superpowers/specs/2026-04-25-benchmark-timestamps-testcount-design.md
git add docs/superpowers/specs/2026-04-25-migration-timing-design.md docs/superpowers/specs/2026-04-25-split-test-projects-design.md
git commit -m "docs: обновить README, CLAUDE.md, run-скрипты и benchmark-issues после удаления TEST_REPEAT"
```

---

## Итоговая проверка

- [ ] `dotnet build` — весь solution без ошибок
- [ ] `dotnet test tests/FastIntegrationTests.Tests.IntegreSQL` — 223 теста, все проходят
- [ ] `Select-String "TEST_REPEAT|TestRepeat" tools,tests --include *.cs -r | Where-Object { $_.Path -notmatch "obj\\" }` — пусто (кроме исторических markdown-файлов)
- [ ] BenchmarkRunner принимает `--scale 2` без ошибок (`dotnet run --project tools/BenchmarkRunner -- --scale 2` до нажатия Enter → проверить вывод Config)
