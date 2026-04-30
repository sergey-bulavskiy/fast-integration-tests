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

    private readonly TimeSpan _cooldown;
    private bool _firstRun = true;

    /// <summary>Инициализирует runner с корневой директорией репозитория, таймаутом и cooldown между прогонами.</summary>
    /// <param name="repoRoot">Корневая директория репозитория.</param>
    /// <param name="timeout">Максимальное время одного прогона dotnet test.</param>
    /// <param name="cooldown">Пауза перед каждым dotnet test, кроме первого — даёт Docker'у время дочистить iptables/NAT после предыдущего процесса.</param>
    public TestRunner(string repoRoot, TimeSpan timeout, TimeSpan cooldown)
    {
        _repoRoot = repoRoot;
        _timeout  = timeout;
        _cooldown = cooldown;
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
            "tests/FastIntegrationTests.Tests.TestcontainersShared",
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
        ApplyCooldown();
        Console.Write(FormatPrefix("[WRM]", scenario));
        var (elapsed, success, output, migrationSeconds, resetSeconds, containerSeconds, cloneSeconds) = RunTest(scenario);
        Console.WriteLine(FormatSuffix(elapsed, success));
        if (!success)
            LogFailure(scenario, output);
        return new BenchmarkResult(scenario, elapsed, migrationSeconds, resetSeconds, containerSeconds, cloneSeconds, success);
    }

    /// <summary>Запускает dotnet test для одного сценария и возвращает результат с временем.</summary>
    public BenchmarkResult Run(BenchmarkScenario scenario)
    {
        ApplyCooldown();
        _currentRun++;
        var tag = _totalRuns > 0 ? $"[{_currentRun,2}/{_totalRuns}]" : "[   ]";
        Console.Write(FormatPrefix(tag, scenario));
        var (elapsed, success, output, migrationSeconds, resetSeconds, containerSeconds, cloneSeconds) = RunTest(scenario);
        Console.WriteLine(FormatSuffix(elapsed, success));
        if (!success)
            LogFailure(scenario, output);
        return new BenchmarkResult(scenario, elapsed, migrationSeconds, resetSeconds, containerSeconds, cloneSeconds, success);
    }

    private (double Elapsed, bool Success, string Output, double MigrationSeconds, double ResetSeconds, double ContainerSeconds, double CloneSeconds) RunTest(BenchmarkScenario scenario)
    {
        var benchLogFile = Path.Combine(
            Path.GetTempPath(), $"bench-{Guid.NewGuid():N}.log");

        var args =
            $"test tests/FastIntegrationTests.Tests.{scenario.Approach}" +
            $" --no-build" +
            $" -- xUnit.MaxParallelThreads={scenario.MaxParallelThreads}";

        var sw = Stopwatch.StartNew();
        var (output, code) = RunCapture("dotnet", args,
            new Dictionary<string, string> { ["BENCH_LOG_FILE"] = benchLogFile });
        sw.Stop();

        WaitForRyukToStop();

        var benchContent = File.Exists(benchLogFile)
            ? File.ReadAllText(benchLogFile)
            : string.Empty;
        if (File.Exists(benchLogFile))
            File.Delete(benchLogFile);

        var (migrationMs, resetMs, containerMs, cloneMs) = ParseBenchLines(benchContent);
        return (sw.Elapsed.TotalSeconds, code == 0, output, migrationMs / 1000.0, resetMs / 1000.0, containerMs / 1000.0, cloneMs / 1000.0);
    }

    private void ApplyCooldown()
    {
        if (_firstRun)
        {
            _firstRun = false;
            return;
        }
        if (_cooldown > TimeSpan.Zero)
            Thread.Sleep(_cooldown);
    }

    // После завершения dotnet test Ryuk ещё живёт в Docker и держит 172.17.0.2:8080.
    // Следующий прогон не может создать свой Ryuk на том же порту → все тесты падают.
    // Опрашиваем docker ps, пока контейнер не исчезнет — не фиксированная пауза, а реальное ожидание.
    private void WaitForRyukToStop(int timeoutSeconds = 60)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            var (dockerOutput, _) = RunCapture("docker", "ps --filter name=testcontainers-ryuk --format {{.ID}}");
            if (string.IsNullOrWhiteSpace(dockerOutput))
                return;
            Thread.Sleep(500);
        }
    }

    private static (long MigrationMs, long ResetMs, long ContainerMs, long CloneMs) ParseBenchLines(string output)
    {
        long migrationMs = 0, resetMs = 0, containerMs = 0, cloneMs = 0;
        foreach (var line in output.Split('\n'))
        {
            var t = line.TrimEnd('\r');
            if (t.StartsWith("##BENCH[migration]=") &&
                long.TryParse(t["##BENCH[migration]=".Length..], out var m))
                migrationMs += m;
            else if (t.StartsWith("##BENCH[reset]=") &&
                long.TryParse(t["##BENCH[reset]=".Length..], out var r))
                resetMs += r;
            else if (t.StartsWith("##BENCH[container]=") &&
                long.TryParse(t["##BENCH[container]=".Length..], out var c))
                containerMs += c;
            else if (t.StartsWith("##BENCH[clone]=") &&
                long.TryParse(t["##BENCH[clone]=".Length..], out var cl))
                cloneMs += cl;
        }
        return (migrationMs, resetMs, containerMs, cloneMs);
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

    private (string Output, int Code) RunCapture(
        string filename, string args,
        IReadOnlyDictionary<string, string>? env = null)
    {
        var psi = new ProcessStartInfo(filename, args)
        {
            WorkingDirectory       = _repoRoot,
            UseShellExecute        = false,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
        };
        if (env is not null)
            foreach (var (k, v) in env)
                psi.Environment[k] = v;

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
