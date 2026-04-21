// tools/BenchmarkRunner/Runner/TestRunner.cs
using System.Diagnostics;
using BenchmarkRunner.Models;

namespace BenchmarkRunner.Runner;

/// <summary>Запускает dotnet test как дочерний процесс и измеряет wall-clock время выполнения.</summary>
class TestRunner
{
    private readonly string _repoRoot;

    /// <summary>Инициализирует runner с корневой директорией репозитория.</summary>
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
        {
            Console.WriteLine($"[BUILD] FAIL: exit code {process.ExitCode}");
            throw new Exception($"Process exited with code {process.ExitCode}: {filename} {args}");
        }
    }
}
