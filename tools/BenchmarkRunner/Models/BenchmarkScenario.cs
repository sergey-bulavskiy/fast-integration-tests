// tools/BenchmarkRunner/Models/BenchmarkScenario.cs
namespace BenchmarkRunner.Models;

/// <summary>
/// Описание сценария бенчмарка.
/// </summary>
/// <param name="Approach">Подход: "IntegreSQL" | "Respawn" | "Testcontainers"</param>
/// <param name="ScenarioName">Тип сценария: "migrations" | "scale" | "parallelism"</param>
/// <param name="MigrationCount">Количество миграций</param>
/// <param name="TestRepeat">Количество повторов теста</param>
/// <param name="MaxParallelThreads">Максимальное количество параллельных потоков</param>
public record BenchmarkScenario(
    string Approach,
    string ScenarioName,
    int MigrationCount,
    int TestRepeat,
    int MaxParallelThreads
);
