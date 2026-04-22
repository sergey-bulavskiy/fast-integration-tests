// tools/BenchmarkRunner/Runner/TestRunner.cs
using System.Diagnostics;
using System.Text;
using BenchmarkRunner.Models;

namespace BenchmarkRunner.Runner;

/// <summary>Запускает dotnet test как дочерний процесс и измеряет wall-clock время выполнения.</summary>
class TestRunner
{
    private readonly string _repoRoot;
    private int _totalRuns;
    private int _currentRun;

    /// <summary>Инициализирует runner с корневой директорией репозитория.</summary>
    public TestRunner(string repoRoot) => _repoRoot = repoRoot;

    /// <summary>Устанавливает общее число прогонов для отображения прогресса.</summary>
    public void SetTotalRuns(int total) => _totalRuns = total;

    /// <summary>Собирает тестовый проект. Вызывается перед первым Run и после изменения миграций.</summary>
    public void Build()
    {
        Console.Write("\n[BUILD] tests/FastIntegrationTests.Tests ... ");
        var (output, code) = RunCapture("dotnet", "build tests/FastIntegrationTests.Tests --nologo -v minimal");
        if (code != 0)
        {
            Console.WriteLine("FAIL");
            PrintOutput(output);
            throw new Exception($"Build failed (exit code {code})");
        }
        Console.WriteLine("OK");
    }

    /// <summary>Warmup-прогон — не учитывается в счётчике прогресса и не сохраняется в отчёт.</summary>
    public BenchmarkResult Warmup(BenchmarkScenario scenario)
    {
        Console.Write(FormatPrefix("[WRM]", scenario));
        var (elapsed, success, output) = RunTest(scenario);
        Console.WriteLine(FormatSuffix(elapsed, success));
        return new BenchmarkResult(scenario, elapsed, success);
    }

    /// <summary>Запускает dotnet test для одного сценария и возвращает результат с временем.</summary>
    public BenchmarkResult Run(BenchmarkScenario scenario)
    {
        _currentRun++;
        var tag = _totalRuns > 0 ? $"[{_currentRun,2}/{_totalRuns}]" : "[   ]";
        Console.Write(FormatPrefix(tag, scenario));
        var (elapsed, success, output) = RunTest(scenario);
        Console.WriteLine(FormatSuffix(elapsed, success));
        if (!success)
            PrintOutput(output);
        return new BenchmarkResult(scenario, elapsed, success);
    }

    private (double Elapsed, bool Success, string Output) RunTest(BenchmarkScenario scenario)
    {
        var filter = $"FullyQualifiedName~Tests.{scenario.Approach}";
        var args =
            $"test tests/FastIntegrationTests.Tests" +
            $" --filter \"{filter}\"" +
            $" --no-build" +
            $" -- xUnit.MaxParallelThreads={scenario.MaxParallelThreads}";

        var sw = Stopwatch.StartNew();
        var (output, code) = RunCapture("dotnet", args, ("TEST_REPEAT", scenario.TestRepeat.ToString()));
        sw.Stop();

        return (sw.Elapsed.TotalSeconds, code == 0, output);
    }

    private static string FormatPrefix(string tag, BenchmarkScenario s) =>
        $"{tag} {s.Approach,-15} {s.ScenarioName,-12} m={s.MigrationCount,3} r={s.TestRepeat,2} t={s.MaxParallelThreads} ...";

    private static string FormatSuffix(double elapsed, bool success) =>
        $" {elapsed,6:F1}s  {(success ? "✓" : "✗ FAIL")}";

    private static void PrintOutput(string output)
    {
        Console.WriteLine("\n── captured output ──────────────────────────────────────");
        Console.WriteLine(output.TrimEnd());
        Console.WriteLine("─────────────────────────────────────────────────────────\n");
    }

    private (string Output, int Code) RunCapture(string filename, string args, (string Key, string Value)? env = null)
    {
        var psi = new ProcessStartInfo(filename, args)
        {
            WorkingDirectory       = _repoRoot,
            UseShellExecute        = false,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
        };
        if (env is { } e)
            psi.Environment[e.Key] = e.Value;

        var sb = new StringBuilder();
        using var process = Process.Start(psi)!;
        process.OutputDataReceived += (_, ev) => { if (ev.Data is not null) sb.AppendLine(ev.Data); };
        process.ErrorDataReceived  += (_, ev) => { if (ev.Data is not null) sb.AppendLine(ev.Data); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        return (sb.ToString(), process.ExitCode);
    }
}
