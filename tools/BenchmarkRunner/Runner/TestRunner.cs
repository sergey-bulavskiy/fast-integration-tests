// tools/BenchmarkRunner/Runner/TestRunner.cs
using System.Collections.Concurrent;
using System.Diagnostics;
using BenchmarkRunner.Models;

namespace BenchmarkRunner.Runner;

/// <summary>Запускает dotnet test как дочерний процесс и измеряет wall-clock время выполнения.</summary>
class TestRunner
{
    private readonly string _repoRoot;
    private readonly TimeSpan _timeout;
    private readonly string _logPath;
    private int _totalRuns;
    private int _currentRun;

    /// <summary>Инициализирует runner с корневой директорией репозитория и таймаутом одного прогона.</summary>
    public TestRunner(string repoRoot, TimeSpan timeout)
    {
        _repoRoot = repoRoot;
        _timeout  = timeout;
        _logPath  = Path.Combine(repoRoot, "benchmark-results", "last-failure.log");
    }

    /// <summary>Устанавливает общее число прогонов для отображения прогресса.</summary>
    public void SetTotalRuns(int total) => _totalRuns = total;

    /// <summary>Собирает все тестовые проекты. Вызывается перед первым Run и после изменения миграций.</summary>
    public void Build()
    {
        var projects = new[]
        {
            "tests/FastIntegrationTests.Tests.IntegreSQL",
            "tests/FastIntegrationTests.Tests.Respawn",
            "tests/FastIntegrationTests.Tests.Testcontainers",
        };

        foreach (var project in projects)
        {
            Console.Write($"\n[BUILD] {project} ... ");
            var (output, code) = RunCapture("dotnet", $"build {project} --nologo -v minimal");
            if (code != 0)
            {
                Console.WriteLine("FAIL");
                var buildScenario = new BenchmarkScenario("build", "build", 0, 0, 0);
                LogFailure(buildScenario, output);
                throw new Exception($"Build failed: {project} (exit code {code})");
            }
            Console.WriteLine("OK");
        }
    }

    /// <summary>Warmup-прогон — не учитывается в счётчике прогресса и не сохраняется в отчёт.</summary>
    public BenchmarkResult Warmup(BenchmarkScenario scenario)
    {
        Console.Write(FormatPrefix("[WRM]", scenario));
        var (elapsed, success, output) = RunTest(scenario);
        Console.WriteLine(FormatSuffix(elapsed, success));
        if (!success)
            LogFailure(scenario, output);
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
            LogFailure(scenario, output);
        return new BenchmarkResult(scenario, elapsed, success);
    }

    private (double Elapsed, bool Success, string Output) RunTest(BenchmarkScenario scenario)
    {
        var args =
            $"test tests/FastIntegrationTests.Tests.{scenario.Approach}" +
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

    private void LogFailure(BenchmarkScenario scenario, string output)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_logPath)!);
        var header =
            $"=== FAILURE: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===" + Environment.NewLine +
            $"Scenario: {scenario.Approach} / {scenario.ScenarioName} / m={scenario.MigrationCount} r={scenario.TestRepeat} t={scenario.MaxParallelThreads}" + Environment.NewLine +
            new string('─', 60) + Environment.NewLine;
        File.WriteAllText(_logPath, header + output.TrimEnd() + Environment.NewLine);
        Console.WriteLine($"  → see {_logPath}");
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

        var lines = new ConcurrentQueue<string>();
        using var process = Process.Start(psi)!;
        process.OutputDataReceived += (_, ev) => { if (ev.Data is not null) lines.Enqueue(ev.Data); };
        process.ErrorDataReceived  += (_, ev) => { if (ev.Data is not null) lines.Enqueue(ev.Data); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var completed = process.WaitForExit(_timeout);
        if (!completed)
        {
            process.Kill(entireProcessTree: true);
            return ($"TIMEOUT: process exceeded {_timeout.TotalMinutes:F0} minutes", -1);
        }

        return (string.Join(Environment.NewLine, lines), process.ExitCode);
    }
}
