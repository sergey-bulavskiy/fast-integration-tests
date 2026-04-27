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
            Console.Write($"\n{DateTime.Now:HH:mm} [BUILD] {project} ... ");
            var (output, code) = RunCapture("dotnet", $"build {project} --nologo -v minimal");
            if (code != 0)
            {
                Console.WriteLine("FAIL");
                var buildScenario = new BenchmarkScenario("build", "build", 0, 0);
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
        var (elapsed, success, output, migrationSeconds, resetSeconds) = RunTest(scenario);
        Console.WriteLine(FormatSuffix(elapsed, success));
        if (!success)
            LogFailure(scenario, output);
        return new BenchmarkResult(scenario, elapsed, migrationSeconds, resetSeconds, success);
    }

    /// <summary>Запускает dotnet test для одного сценария и возвращает результат с временем.</summary>
    public BenchmarkResult Run(BenchmarkScenario scenario)
    {
        _currentRun++;
        var tag = _totalRuns > 0 ? $"[{_currentRun,2}/{_totalRuns}]" : "[   ]";
        Console.Write(FormatPrefix(tag, scenario));
        var (elapsed, success, output, migrationSeconds, resetSeconds) = RunTest(scenario);
        Console.WriteLine(FormatSuffix(elapsed, success));
        if (!success)
            LogFailure(scenario, output);
        return new BenchmarkResult(scenario, elapsed, migrationSeconds, resetSeconds, success);
    }

    private (double Elapsed, bool Success, string Output, double MigrationSeconds, double ResetSeconds) RunTest(BenchmarkScenario scenario)
    {
        var args =
            $"test tests/FastIntegrationTests.Tests.{scenario.Approach}" +
            $" --no-build" +
            $" -- xUnit.MaxParallelThreads={scenario.MaxParallelThreads}";

        var sw = Stopwatch.StartNew();
        var (output, code) = RunCapture("dotnet", args);
        sw.Stop();

        var (migrationMs, resetMs) = ParseBenchLines(output);
        return (sw.Elapsed.TotalSeconds, code == 0, output, migrationMs / 1000.0, resetMs / 1000.0);
    }

    private static (long MigrationMs, long ResetMs) ParseBenchLines(string output)
    {
        long migrationMs = 0, resetMs = 0;
        foreach (var line in output.Split('\n'))
        {
            var t = line.TrimEnd('\r');
            if (t.StartsWith("##BENCH[migration]=") &&
                long.TryParse(t["##BENCH[migration]=".Length..], out var m))
                migrationMs += m;
            else if (t.StartsWith("##BENCH[reset]=") &&
                long.TryParse(t["##BENCH[reset]=".Length..], out var r))
                resetMs += r;
        }
        return (migrationMs, resetMs);
    }

    private static string FormatPrefix(string tag, BenchmarkScenario s) =>
        $"{DateTime.Now:HH:mm} {tag} {s.Approach,-15} {s.ScenarioName,-12} m={s.MigrationCount,3} s={s.ClassScale,2} t={s.MaxParallelThreads} ...";

    private static string FormatSuffix(double elapsed, bool success) =>
        $" {elapsed,6:F1}s  {(success ? "✓" : "✗ FAIL")}";

    private void LogFailure(BenchmarkScenario scenario, string output)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_logPath)!);
        var header =
            $"=== FAILURE: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===" + Environment.NewLine +
            $"Scenario: {scenario.Approach} / {scenario.ScenarioName} / m={scenario.MigrationCount} s={scenario.ClassScale} t={scenario.MaxParallelThreads}" + Environment.NewLine +
            new string('─', 60) + Environment.NewLine;
        File.WriteAllText(_logPath, header + output.TrimEnd() + Environment.NewLine);
        Console.WriteLine($"  → see {_logPath}");
    }

    private (string Output, int Code) RunCapture(string filename, string args)
    {
        var psi = new ProcessStartInfo(filename, args)
        {
            WorkingDirectory       = _repoRoot,
            UseShellExecute        = false,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
        };
        // Быстрое цикличное создание/уничтожение контейнеров (336 шт. при scale=12) исчерпывает
        // iptables-правила Docker bridge-сети на мощных машинах: IP переиспользуется раньше,
        // чем старые правила очищаются → "address already in use" для postgresql:5432.
        // Ryuk добавляет асинхронный слой очистки, который усугубляет гонку.
        // DisposeAsync в фикстурах обеспечивает синхронную очистку — Ryuk не нужен.
        psi.Environment["TESTCONTAINERS_RYUK_DISABLED"] = "true";

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
