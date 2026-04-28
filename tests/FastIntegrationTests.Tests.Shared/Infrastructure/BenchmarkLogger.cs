using System.Collections.Concurrent;

namespace FastIntegrationTests.Tests.Infrastructure;

/// <summary>
/// Записывает ##BENCH маркеры в файл без блокировок в горячем пути.
/// Console.WriteLine не работает — xUnit перехватывает Console.Out через Console.SetOut().
/// Путь к файлу передаётся через BENCH_LOG_FILE env var (устанавливается BenchmarkRunner).
/// При отсутствии env var — no-op: работает вне бенчмарка без побочных эффектов.
/// </summary>
public static class BenchmarkLogger
{
    private static readonly ConcurrentQueue<string> _lines = new();
    private static readonly string? _path =
        Environment.GetEnvironmentVariable("BENCH_LOG_FILE");

    static BenchmarkLogger()
    {
        if (_path is not null)
            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
                File.WriteAllLines(_path, _lines);
    }

    /// <summary>Добавляет строку ##BENCH[key]=ms в очередь (lock-free).</summary>
    /// <param name="key">Ключ маркера: <c>migration</c>, <c>reset</c>, <c>container</c> или <c>clone</c>.</param>
    /// <param name="ms">Время в миллисекундах.</param>
    public static void Write(string key, long ms)
    {
        if (_path is null) return;
        _lines.Enqueue($"##BENCH[{key}]={ms}");
    }
}
