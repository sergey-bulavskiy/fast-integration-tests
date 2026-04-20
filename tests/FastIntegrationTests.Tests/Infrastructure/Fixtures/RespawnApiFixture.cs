namespace FastIntegrationTests.Tests.Infrastructure.Fixtures;

/// <summary>
/// Расширяет <see cref="RespawnFixture"/> для HTTP-тестов: создаёт
/// <see cref="TestWebApplicationFactory"/> и <see cref="HttpClient"/> один раз на класс.
/// Данные сбрасываются перед каждым тестом через Respawn, TestServer при этом не пересоздаётся.
/// </summary>
public sealed class RespawnApiFixture : RespawnFixture
{
    private TestWebApplicationFactory _factory = null!;

    /// <summary>HTTP-клиент, общий для всех тестов класса.</summary>
    public HttpClient Client { get; private set; } = null!;

    /// <inheritdoc />
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _factory = new TestWebApplicationFactory(ConnectionString);
        Client = _factory.CreateClient();
    }

    /// <inheritdoc />
    public override async Task DisposeAsync()
    {
        Client?.Dispose();
        if (_factory is not null) await _factory.DisposeAsync();
        await base.DisposeAsync();
    }
}
