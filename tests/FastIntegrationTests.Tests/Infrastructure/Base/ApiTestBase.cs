namespace FastIntegrationTests.Tests.Infrastructure.Base;

/// <summary>
/// Базовый класс для интеграционных тестов HTTP-уровня.
/// Создаёт изолированную БД на каждый тест и поднимает TestWebApplicationFactory.
/// </summary>
public abstract class ApiTestBase : IAsyncLifetime
{
    private readonly ContainerFixture _fixture;
    private ShopDbContext _schemaContext = null!;
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
        _schemaContext = await dbFactory.CreateAsync();

        // Передаём строку подключения к уже готовой БД в фабрику приложения
        var connectionString = _schemaContext.Database.GetConnectionString()!;
        _factory = new TestWebApplicationFactory(_fixture.Provider, connectionString);
        Client = _factory.CreateClient();
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        Client?.Dispose();
        if (_factory is not null) await _factory.DisposeAsync();
        if (_schemaContext is null) return;
        await _schemaContext.Database.EnsureDeletedAsync();
        await _schemaContext.DisposeAsync();
    }
}
