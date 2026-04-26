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

const int BaseMigrations   = 17;   // реальных миграций в репозитории
const int MaxMigrations    = 117;  // с benchmark-миграциями — базовое состояние бенчмарка
var approaches             = new[] { "IntegreSQL", "Respawn", "Testcontainers" };
var migrationCounts        = new[] { BaseMigrations, 42, 67, 92, MaxMigrations };
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

// Восстановить скрытые миграции и убрать scale-классы после возможного прерванного прогона
migrationManager.RestoreHiddenMigrations();
classScaleManager.RemoveScaleClasses();

// Первичная сборка
runner.Build();

try
{
    // ─── Warmup ─────────────────────────────────────────────────────────────────
    Console.WriteLine($"\n{DateTime.Now:HH:mm} ═══ Warmup (не входит в отчёт) ═══");
    foreach (var approach in approaches)
    {
        var warmup = runner.Warmup(new BenchmarkScenario(approach, "warmup", MaxMigrations, defaultThreads));
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
            var toHide = MaxMigrations - migrationCount;
            try
            {
                if (toHide > 0)
                {
                    migrationManager.HideMigrations(toHide);
                    runner.Build();
                }
                foreach (var approach in approaches)
                    RunOrAbort(new BenchmarkScenario(approach, "migrations", migrationCount, defaultThreads, defaultClassScale));
            }
            finally
            {
                if (toHide > 0)
                {
                    migrationManager.RestoreHiddenMigrations();
                    runner.Build();
                }
            }
        }
    }
    finally
    {
        try { classScaleManager.RemoveScaleClasses(); runner.Build(); }
        catch (Exception ex) { Console.Error.WriteLine($"[warn] cleanup failed: {ex.Message}"); }
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
                RunOrAbort(new BenchmarkScenario(approach, "scale", MaxMigrations, defaultThreads, scale));
        }
        finally
        {
            if (scale > 1)
            {
                try { classScaleManager.RemoveScaleClasses(); runner.Build(); }
                catch (Exception ex) { Console.Error.WriteLine($"[warn] cleanup failed: {ex.Message}"); }
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
                RunOrAbort(new BenchmarkScenario(approach, "parallelism", MaxMigrations, parallelism, defaultClassScale));
    }
    finally
    {
        try { classScaleManager.RemoveScaleClasses(); runner.Build(); }
        catch (Exception ex) { Console.Error.WriteLine($"[warn] cleanup failed: {ex.Message}"); }
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
