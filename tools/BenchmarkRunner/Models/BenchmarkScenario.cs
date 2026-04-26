// tools/BenchmarkRunner/Models/BenchmarkScenario.cs
namespace BenchmarkRunner.Models;

/// <summary>
/// Описание сценария бенчмарка.
/// </summary>
/// <param name="Approach">Подход: "IntegreSQL" | "Respawn" | "Testcontainers"</param>
/// <param name="ScenarioName">Тип сценария: "migrations" | "scale" | "parallelism"</param>
/// <param name="MigrationCount">Количество миграций</param>
/// <param name="MaxParallelThreads">Максимальное количество параллельных потоков</param>
/// <param name="ClassScale">Множитель числа тест-классов (по умолчанию 1)</param>
public record BenchmarkScenario(
    string Approach,
    string ScenarioName,
    int MigrationCount,
    int MaxParallelThreads,
    int ClassScale = 1
);
