namespace FastIntegrationTests.Tests.Infrastructure.Base;

/// <summary>
/// Базовый класс для интеграционных тестов HTTP-уровня.
/// Создаёт изолированную БД на каждый тест и поднимает TestWebApplicationFactory.
/// </summary>
public abstract class ApiTestBase : IAsyncLifetime
{
    private readonly ContainerFixture _fixture;
    private ShopDbContext _context = null!;
    private TestWebApplicationFactory _factory = null!;

    /// <summary>HTTP-клиент для обращений к тестируемому API.</summary>
    protected HttpClient Client { get; private set; } = null!;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="ApiTestBase"/>.
    /// </summary>
    /// <param name="fixture">Запущенный контейнер с СУБД.</param>
    protected ApiTestBase(ContainerFixture fixture) => _fixture = fixture;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        // Создаём изолированную БД и применяем миграции
        var dbFactory = new TestDbFactory(_fixture);
        _context = await dbFactory.CreateAsync();

        // Передаём строку подключения к уже готовой БД в фабрику приложения
        var connectionString = _context.Database.GetConnectionString()!;
        _factory = new TestWebApplicationFactory(connectionString);
        Client = _factory.CreateClient();
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_context is null) return;
        Client?.Dispose();
        if (_factory is not null)
            // PhysicalFilesWatcher внутри WebApplicationFactory может бросить NullReferenceException
            // при диспозе под высокой параллельностью — баг в ASP.NET Core FileSystemWatcher.
            try { await _factory.DisposeAsync(); } catch (NullReferenceException) { }
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await _context.Database.EnsureDeletedAsync();
        sw.Stop();
        BenchmarkLogger.Write("reset", sw.ElapsedMilliseconds);
        await _context.DisposeAsync();
    }
}
