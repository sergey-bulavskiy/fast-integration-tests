// tools/BenchmarkRunner/Program.cs
using BenchmarkRunner.Migrations;
using BenchmarkRunner.Models;
using BenchmarkRunner.Runner;

var repoRoot         = FindRepoRoot();
var runner           = new TestRunner(repoRoot);
var migrationManager = new MigrationManager(repoRoot);

// Добавить 3 фейковые миграции, убедиться что файлы создались, затем удалить
migrationManager.AddFakeMigrations(3);

var migrationsDir = Path.Combine(repoRoot, "src", "FastIntegrationTests.Infrastructure", "Migrations");
var fakeFiles     = Directory.GetFiles(migrationsDir, "*Benchmark_Fake*");
Console.WriteLine($"\nFake files created: {fakeFiles.Length}");
foreach (var f in fakeFiles) Console.WriteLine($"  {Path.GetFileName(f)}");

// Проверяем что сгенерированный C# синтаксически корректен
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
