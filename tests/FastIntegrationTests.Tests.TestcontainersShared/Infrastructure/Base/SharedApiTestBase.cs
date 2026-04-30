namespace FastIntegrationTests.Tests.Infrastructure.Base;

/// <summary>
/// Базовый класс для интеграционных тестов HTTP-уровня через TestcontainersShared.
/// Контейнер общий на процесс; БД и TestServer создаются на каждый тест.
/// </summary>
public abstract class SharedApiTestBase : IAsyncLifetime
{
    private readonly SharedDbHandle _db = new();
    private TestWebApplicationFactory _factory = null!;

    /// <summary>HTTP-клиент для обращений к тестируемому API.</summary>
    protected HttpClient Client { get; private set; } = null!;

    /// <inheritdoc />
    public virtual async Task InitializeAsync()
    {
        await _db.CreateAndMigrateAsync();
        _factory = new TestWebApplicationFactory(_db.ConnectionString);
        Client = _factory.CreateClient();
    }

    /// <inheritdoc />
    public virtual async Task DisposeAsync()
    {
        Client?.Dispose();
        if (_factory is not null)
            // PhysicalFilesWatcher внутри WebApplicationFactory может бросить NullReferenceException
            // при диспозе под высокой параллельностью — баг в ASP.NET Core FileSystemWatcher.
            try { await _factory.DisposeAsync(); } catch (NullReferenceException) { }
        await _db.DropAsync();
    }
}
