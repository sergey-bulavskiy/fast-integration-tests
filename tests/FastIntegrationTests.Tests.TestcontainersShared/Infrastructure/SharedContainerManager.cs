using System.Diagnostics;
using Testcontainers.PostgreSql;

namespace FastIntegrationTests.Tests.Infrastructure;

/// <summary>
/// Запускает один PostgreSQL-контейнер один раз на весь процесс.
/// Все тесты разделяют этот контейнер; каждый создаёт свою БД через <see cref="SharedDbHandle"/>.
/// </summary>
/// <remarks>
/// Контейнер не останавливается явно — Ryuk-агент Testcontainers убирает его после завершения процесса.
/// </remarks>
public static class SharedContainerManager
{
    private static readonly Lazy<Task<PostgreSqlContainer>> _container =
        new(() => StartAsync(), LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// Возвращает запущенный контейнер. Первый вызов стартует контейнер;
    /// последующие возвращают кешированный результат.
    /// </summary>
    public static Task<PostgreSqlContainer> GetContainerAsync() => _container.Value;

    private static async Task<PostgreSqlContainer> StartAsync()
    {
        // Ryuk от предыдущего dotnet test (или soak'а) мог не успеть дочистить
        // сеть/контейнеры. На быстрых машинах Docker иначе переиспользует IP до
        // того, как iptables очистит правила → "address already in use".
        await Task.Delay(TimeSpan.FromSeconds(10));

        // Параметры производительности PostgreSQL для тестовой среды.
        // Рекомендованы авторами IntegreSQL:
        // https://github.com/allaboutapps/integresql/blob/master/README.md
        // ⚠ НИКОГДА не переносить в продакшн — при сбое питания возможна потеря данных.
        var container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithCommand(
                // 195 тестов × scale=50 = ~9750 уникальных connection strings →
                // ~9750 пулов в одном процессе. Дефолт max_connections=100 заведомо мало.
                "-c", "max_connections=500",
                "-c", "fsync=off",
                "-c", "synchronous_commit=off",
                "-c", "full_page_writes=off",
                "-c", "shared_buffers=128MB"
            )
            .Build();

        var sw = Stopwatch.StartNew();
        await container.StartAsync();
        sw.Stop();
        BenchmarkLogger.Write("container", sw.ElapsedMilliseconds);

        // Новому Ryuk нужно успеть полностью подняться, иначе первые тесты
        // могут упереться в незавершённый init.
        await Task.Delay(TimeSpan.FromSeconds(10));

        return container;
    }
}
