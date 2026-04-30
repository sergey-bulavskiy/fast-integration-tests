using System.Diagnostics;
using DotNet.Testcontainers.Builders;
using Testcontainers.PostgreSql;

namespace FastIntegrationTests.Tests.Infrastructure;

/// <summary>
/// Запускает один PostgreSQL-контейнер в Docker один раз на весь процесс.
/// Все тест-классы Respawn разделяют этот контейнер; каждый создаёт свою БД.
/// </summary>
/// <remarks>
/// Контейнер не останавливается явно — Ryuk-агент Testcontainers убирает его после завершения процесса.
/// </remarks>
public static class RespawnContainerManager
{
    private static readonly Lazy<Task<PostgreSqlContainer>> _container =
        new(() => StartAsync(), LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// Возвращает запущенный контейнер.
    /// При первом вызове стартует контейнер; последующие возвращают кешированный результат.
    /// </summary>
    public static Task<PostgreSqlContainer> GetContainerAsync() => _container.Value;

    private static async Task<PostgreSqlContainer> StartAsync()
    {
        // Изолированная сеть на менеджер. Внутри Respawn-процесса контейнер один,
        // но сеть всё равно полезна: при network.DisposeAsync() Docker убирает
        // все iptables-правила сети атомарно, не оставляя стейлов для следующего
        // процесса. И симметрично с IntegresSqlContainerManager / ContainerFixture.
        var network = new NetworkBuilder().Build();
        await network.CreateAsync();

        // Параметры производительности PostgreSQL для тестовой среды.
        // Рекомендованы авторами IntegreSQL:
        // https://github.com/allaboutapps/integresql/blob/master/README.md
        // ⚠ НИКОГДА не переносить в продакшн — при сбое питания возможна потеря данных.
        var container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithNetwork(network)
            .WithCommand(
                // 14 параллельных классов × connection pool — дефолтных 100 не хватает.
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

        // Пауза ПОСЛЕ старта контейнера. await StartAsync() возвращается, когда
        // Docker рапортует "процесс в контейнере запущен", но NAT-правила и port
        // forwarding на хосте прописываются ещё ~сотни мс. Если первый коннект
        // уйдёт в это окно — получит TCP RST до того, как правило вступило в силу.
        // Пауза гарантирует, что коннекты пойдут уже по живому NAT.
        await Task.Delay(TimeSpan.FromSeconds(10));

        return container;
    }
}
