// tools/BenchmarkRunner/Models/BenchmarkResult.cs
namespace BenchmarkRunner.Models;

/// <summary>
/// Результат одного бенчмарка.
/// </summary>
/// <param name="Scenario">Сценарий бенчмарка</param>
/// <param name="ElapsedSeconds">Время выполнения в секундах</param>
/// <param name="Success">Успешное ли выполнение</param>
public record BenchmarkResult(
    BenchmarkScenario Scenario,
    double ElapsedSeconds,
    bool Success
);
