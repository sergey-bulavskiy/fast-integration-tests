namespace FastIntegrationTests.Tests.Infrastructure.Base;

/// <summary>
/// Базовый класс для интеграционных тестов HTTP-уровня через Respawn.
/// TestServer и HttpClient создаются один раз на класс; данные сбрасываются перед каждым тестом.
/// </summary>
public abstract class RespawnApiTestBase : IAsyncLifetime, IClassFixture<RespawnApiFixture>
{
    private readonly RespawnApiFixture _fixture;

    /// <summary>HTTP-клиент для обращений к тестируемому API.</summary>
    protected HttpClient Client => _fixture.Client;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="RespawnApiTestBase"/>.
    /// </summary>
    /// <param name="fixture">Фикстура с контейнером, Respawner и HTTP-клиентом.</param>
    protected RespawnApiTestBase(RespawnApiFixture fixture) => _fixture = fixture;

    /// <inheritdoc />
    public virtual async Task InitializeAsync() => await _fixture.ResetAsync();

    /// <inheritdoc />
    public virtual Task DisposeAsync() => Task.CompletedTask;
}
