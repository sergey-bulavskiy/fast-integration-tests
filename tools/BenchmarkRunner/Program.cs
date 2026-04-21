// tools/BenchmarkRunner/Program.cs
using BenchmarkRunner.Models;
using BenchmarkRunner.Report;

var repoRoot  = FindRepoRoot();
var reportGen = new ReportGenerator(repoRoot);

var fakeReport = new BenchmarkReport(
    DateTime.UtcNow,
    Environment.MachineName,
    new List<BenchmarkResult>
    {
        new(new BenchmarkScenario("IntegreSQL",     "migrations", 17,  10, 4), 12.3,  true),
        new(new BenchmarkScenario("Respawn",        "migrations", 17,  10, 4),  9.1,  true),
        new(new BenchmarkScenario("Testcontainers", "migrations", 17,  10, 4), 45.6,  true),
        new(new BenchmarkScenario("IntegreSQL",     "migrations", 67,  10, 4), 13.1,  true),
        new(new BenchmarkScenario("Respawn",        "migrations", 67,  10, 4), 11.2,  true),
        new(new BenchmarkScenario("Testcontainers", "migrations", 67,  10, 4),145.0,  true),
        new(new BenchmarkScenario("IntegreSQL",     "migrations", 117, 10, 4), 13.5,  true),
        new(new BenchmarkScenario("Respawn",        "migrations", 117, 10, 4), 13.8,  true),
        new(new BenchmarkScenario("Testcontainers", "migrations", 117, 10, 4),245.0,  true),
        new(new BenchmarkScenario("IntegreSQL",     "scale",      17,   1, 4),  2.1,  true),
        new(new BenchmarkScenario("Respawn",        "scale",      17,   1, 4),  1.8,  true),
        new(new BenchmarkScenario("Testcontainers", "scale",      17,   1, 4),  4.5,  true),
        new(new BenchmarkScenario("IntegreSQL",     "scale",      17,  20, 4), 18.0,  true),
        new(new BenchmarkScenario("Respawn",        "scale",      17,  20, 4), 15.0,  true),
        new(new BenchmarkScenario("Testcontainers", "scale",      17,  20, 4), 88.0,  true),
        new(new BenchmarkScenario("IntegreSQL",     "parallelism",17,  20, 1), 55.0,  true),
        new(new BenchmarkScenario("Respawn",        "parallelism",17,  20, 1), 60.0,  true),
        new(new BenchmarkScenario("Testcontainers", "parallelism",17,  20, 1),180.0,  true),
        new(new BenchmarkScenario("IntegreSQL",     "parallelism",17,  20, 8), 12.0,  true),
        new(new BenchmarkScenario("Respawn",        "parallelism",17,  20, 8), 55.0,  true),
        new(new BenchmarkScenario("Testcontainers", "parallelism",17,  20, 8), 80.0,  true),
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
