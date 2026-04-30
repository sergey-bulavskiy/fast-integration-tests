using NUnit.Framework;

[assembly: Parallelizable(ParallelScope.Fixtures)]
[assembly: LevelOfParallelism(12)]

// Альтернатива: ParallelScope.All — параллелизм и внутри классов.
// IntegreSQL даёт изоляцию на уровне теста (каждый тест получает свой клон БД),
// поэтому ParallelScope.All валиден. Текущий выбор Fixtures соответствует
// поведению xUnit-версии (классы параллельно, тесты внутри — последовательно)
// и упрощает сравнение «один в один».
// [assembly: Parallelizable(ParallelScope.All)]
