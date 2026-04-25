// tools/BenchmarkRunner/Models/BenchmarkReport.cs
namespace BenchmarkRunner.Models;

/// <summary>
/// Отчёт о результатах бенчмарков.
/// </summary>
/// <param name="GeneratedAt">Дата и время создания отчёта</param>
/// <param name="MachineName">Имя машины, на которой запускались тесты</param>
/// <param name="BaseTestCount">Базовое количество тест-методов в одном подходе (хардкод)</param>
/// <param name="Results">Коллекция результатов бенчмарков</param>
public record BenchmarkReport(
    DateTime GeneratedAt,
    string MachineName,
    int BaseTestCount,
    IReadOnlyList<BenchmarkResult> Results
);
