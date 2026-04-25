// tools/BenchmarkRunner/Program.cs
using BenchmarkRunner.Migrations;
using BenchmarkRunner.Models;
using BenchmarkRunner.Report;
using BenchmarkRunner.Runner;

// ─── Аргументы командной строки ────────────────────────────────────────────
int defaultThreads = 8;  // для сценариев, где потоки не варьируются (1 и 2)
int defaultRepeat  = 12; // для сценариев, где повторы не варьируются (1 и 3)
int timeoutMinutes = 50; // таймаут одного прогона dotnet test

// хардкод — обновить при добавлении/удалении тест-методов в тест-проектах
// проверить: dotnet test tests/FastIntegrationTests.Tests.IntegreSQL --list-tests 2>&1 | grep "FastIntegrationTests.Tests.IntegreSQL" | wc -l
const int BaseTestCount = 223;

for (var i = 0; i < args.Length - 1; i++)
{
    if (args[i] is "--threads" or "-t" && int.TryParse(args[i + 1], out var t) && t > 0)
        defaultThreads = t;
    if (args[i] is "--repeat" or "-r" && int.TryParse(args[i + 1], out var r) && r > 0)
        defaultRepeat = r;
    if (args[i] is "--timeout" && int.TryParse(args[i + 1], out var to) && to > 0)
        timeoutMinutes = to;
}

var repoRoot         = FindRepoRoot();
var runner           = new TestRunner(repoRoot, TimeSpan.FromMinutes(timeoutMinutes));
var migrationManager = new MigrationManager(repoRoot);
var reportGenerator  = new ReportGenerator(repoRoot);
var results          = new List<BenchmarkResult>();

const int BaseMigrations = 17;
var approaches         = new[] { "IntegreSQL", "Respawn", "Testcontainers" };
var migrationCounts    = new[] { 17, 42, 67, 92, 117 };
var scalingRepeats     = new[] { 1, 5, 10, 20, 50 };
var parallelismThreads = new[] { 1, 2, 4, 8 };
runner.SetTotalRuns((migrationCounts.Length + scalingRepeats.Length + parallelismThreads.Length) * approaches.Length);

Console.WriteLine("=== Integration Test Benchmark Runner ===");
Console.WriteLine($"Repo:    {repoRoot}");
Console.WriteLine($"Machine: {Environment.MachineName}");
Console.WriteLine($"Time:    {DateTime.Now:yyyy-MM-dd HH:mm}");
Console.WriteLine($"Config:  threads={defaultThreads}, repeat={defaultRepeat}, timeout={timeoutMinutes}m");
Console.WriteLine("\nDocker must be running. Full run takes ~1-2 hours.");
Console.WriteLine("Press Enter to start, Ctrl+C to cancel...");
Console.ReadLine();

// Убрать фейковые миграции, которые могли остаться от прерванного прогона
migrationManager.RemoveFakeMigrations();

// Первичная сборка
runner.Build();

try
{
    // ─── Warmup: разогреть Docker + JIT для всех трёх подходов ─────────────────
    // Каждый подход запускается по одному разу (TEST_REPEAT=1), результаты не сохраняются.
    // Цель: вытащить Docker-образы, прогреть JIT теста и container-startup каждого подхода
    // до первой измеряемой точки Сценария 1.
    Console.WriteLine("\n═══ Warmup (не входит в отчёт) ═══");
    foreach (var approach in approaches)
    {
        var warmup = runner.Warmup(new BenchmarkScenario(approach, "warmup", BaseMigrations, TestRepeat: 1, MaxParallelThreads: defaultThreads));
        if (!warmup.Success)
            throw new BenchmarkAbortedException();
    }

    // ─── Сценарий 1: влияние числа миграций ────────────────────────────────────
    Console.WriteLine("\n═══ Scenario 1: Migration Count Impact ═══");
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
                RunOrAbort(new BenchmarkScenario(approach, "migrations", migrationCount, TestRepeat: defaultRepeat, MaxParallelThreads: defaultThreads));
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

    // ─── Сценарий 2: масштаб числа тестов ──────────────────────────────────────
    Console.WriteLine("\n═══ Scenario 2: Test Count Scaling ═══");
    foreach (var repeat in scalingRepeats)
        foreach (var approach in approaches)
            RunOrAbort(new BenchmarkScenario(approach, "scale", BaseMigrations, TestRepeat: repeat, MaxParallelThreads: defaultThreads));

    // ─── Сценарий 3: параллелизм ────────────────────────────────────────────────
    Console.WriteLine("\n═══ Scenario 3: Parallelism ═══");
    foreach (var parallelism in parallelismThreads)
        foreach (var approach in approaches)
            RunOrAbort(new BenchmarkScenario(approach, "parallelism", BaseMigrations, TestRepeat: defaultRepeat, MaxParallelThreads: parallelism));
}
catch (BenchmarkAbortedException)
{
    Console.WriteLine("\n=== BENCHMARK ABORTED ===");
    Console.WriteLine("Fix the failing tests and re-run the benchmark.");
    Environment.Exit(1);
}

// ─── Генерация отчёта ───────────────────────────────────────────────────────
var report = new BenchmarkReport(DateTime.UtcNow, Environment.MachineName, BaseTestCount, results);
reportGenerator.Generate(report);

Console.WriteLine("\n=== Done! ===");
Console.WriteLine("Open: benchmark-results/report.html");

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
