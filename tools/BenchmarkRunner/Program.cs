// tools/BenchmarkRunner/Program.cs
using BenchmarkRunner.Models;
using BenchmarkRunner.Runner;

var repoRoot = FindRepoRoot();
var runner   = new TestRunner(repoRoot);

runner.Build();

var result = runner.Run(new BenchmarkScenario("IntegreSQL", "smoke", 17, 1, 4));
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
