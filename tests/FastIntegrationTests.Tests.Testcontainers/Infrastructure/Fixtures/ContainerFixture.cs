using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Networks;
using Testcontainers.PostgreSql;

namespace FastIntegrationTests.Tests.Infrastructure.Fixtures;

/// <summary>
/// Запускает один Testcontainers-контейнер PostgreSQL на всю коллекцию тестов.
/// </summary>
public sealed class ContainerFixture : IAsyncLifetime
{
    private INetwork _network = null!;
    private PostgreSqlContainer _container = null!;

    /// <summary>Базовая строка подключения к контейнеру (без конкретной БД).</summary>
    public string ConnectionString { get; private set; } = null!;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        // Предыдущий Ryuk мог не успеть дочистить сеть до начала новой инициализации.
        await Task.Delay(TimeSpan.FromSeconds(10));

        // Изолированная сеть на каждую фикстуру — без неё Docker переиспользует IP (172.17.0.x)
        // для новых контейнеров быстрее, чем iptables успевает очистить правила предыдущих.
        // На мощных машинах с быстрым оборотом фикстур это приводит к "address already in use".
        var sw = System.Diagnostics.Stopwatch.StartNew();
        _network = new NetworkBuilder().Build();
        await _network.CreateAsync();

        // Параметры производительности PostgreSQL для тестовой среды.
        // Рекомендованы авторами IntegreSQL в официальном docker-compose.yml:
        // https://github.com/allaboutapps/integresql/blob/master/README.md
        // Совокупно дают ~30% ускорение за счёт отключения гарантий долговечности WAL,
        // которые необходимы в продакшне, но бессмысленны для эфемерных тестовых данных.
        // ⚠ НИКОГДА не переносить в продакшн — при сбое питания/краше возможна потеря данных.
        _container = new PostgreSqlBuilder()
            .WithNetwork(_network)
            .WithImage("postgres:16-alpine")
            .WithCommand(
                // fsync=off: PostgreSQL не вызывает fsync() для сброса WAL на диск.
                // В продакшне защищает от потери коммитов при сбое питания.
                // В тесте контейнер эфемерный — защита не нужна, а ожидание IO — главный тормоз.
                // Docs: https://www.postgresql.org/docs/current/runtime-config-wal.html#GUC-FSYNC
                "-c", "fsync=off",
                // synchronous_commit=off: сервер подтверждает транзакцию клиенту не дожидаясь
                // записи WAL на диск. С fsync=off основной эффект уже достигнут, но явное
                // отключение дополнительно убирает задержки синхронизации со standby-репликами.
                // Docs: https://www.postgresql.org/docs/current/runtime-config-wal.html#GUC-SYNCHRONOUS-COMMIT
                "-c", "synchronous_commit=off",
                // full_page_writes=off: PostgreSQL не записывает полную страницу в WAL после
                // чекпоинта. С fsync=off частичная запись страниц невозможна, поэтому флаг
                // избыточен. Отключение снижает объём WAL-записей.
                // Docs: https://www.postgresql.org/docs/current/runtime-config-wal.html#GUC-FULL-PAGE-WRITES
                "-c", "full_page_writes=off",
                // shared_buffers=128MB: размер общего буферного кеша. Дефолт в alpine-образе
                // — 32MB. 128MB снижает количество дисковых чтений при повторных обращениях
                // к одним страницам между тестами.
                // Docs: https://www.postgresql.org/docs/current/runtime-config-resource.html#GUC-SHARED-BUFFERS
                "-c", "shared_buffers=128MB"
            )
            .Build();
        await _container.StartAsync();
        sw.Stop();
        BenchmarkLogger.Write("container", sw.ElapsedMilliseconds);
        ConnectionString = _container.GetConnectionString();

        // Ryuk от предыдущей фикстуры дочищает сети асинхронно после DisposeAsync,
        // а новый Ryuk не всегда успевает полностью подняться к моменту старта тестов.
        // Пауза даёт время обоим завершить инициализацию/очистку.
        await Task.Delay(TimeSpan.FromSeconds(10));
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
        if (_network is not null)
            await _network.DisposeAsync();
    }
}
