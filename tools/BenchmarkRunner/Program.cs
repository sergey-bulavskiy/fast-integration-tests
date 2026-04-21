// tools/BenchmarkRunner/Program.cs
using BenchmarkRunner.Migrations;
using BenchmarkRunner.Models;
using BenchmarkRunner.Report;
using BenchmarkRunner.Runner;

var repoRoot         = FindRepoRoot();
var runner           = new TestRunner(repoRoot);
var migrationManager = new MigrationManager(repoRoot);
var results          = new List<BenchmarkResult>();

const int BaseMigrations = 17;
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
foreach (var migrationCount in new[] { 17, 67, 117 })
{
    var fakesToAdd = migrationCount - BaseMigrations;
    if (fakesToAdd > 0)
    {
        migrationManager.AddFakeMigrations(fakesToAdd);
        runner.Build();
    }

    foreach (var approach in approaches)
        results.Add(runner.Run(
            new BenchmarkScenario(approach, "migrations", migrationCount, TestRepeat: 10, MaxParallelThreads: 4)));

    if (fakesToAdd > 0)
    {
        migrationManager.RemoveFakeMigrations();
        runner.Build();
    }
}

// ─── Сценарий 2: масштаб числа тестов ──────────────────────────────────────
Console.WriteLine("\n═══ Scenario 2: Test Count Scaling ═══");
foreach (var repeat in new[] { 1, 5, 10, 20, 50 })
    foreach (var approach in approaches)
        results.Add(runner.Run(
            new BenchmarkScenario(approach, "scale", BaseMigrations, TestRepeat: repeat, MaxParallelThreads: 4)));

// ─── Сценарий 3: параллелизм ────────────────────────────────────────────────
Console.WriteLine("\n═══ Scenario 3: Parallelism ═══");
foreach (var threads in new[] { 1, 2, 4, 8 })
    foreach (var approach in approaches)
        results.Add(runner.Run(
            new BenchmarkScenario(approach, "parallelism", BaseMigrations, TestRepeat: 20, MaxParallelThreads: threads)));

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
