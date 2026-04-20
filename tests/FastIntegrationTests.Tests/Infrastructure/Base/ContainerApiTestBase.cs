namespace FastIntegrationTests.Tests.Infrastructure.Base;

/// <summary>
/// Базовый класс для интеграционных тестов HTTP-уровня через Testcontainers.
/// Объявляет <see cref="IClassFixture{ContainerFixture}"/>, поэтому наследники автоматически
/// получают отдельный контейнер PostgreSQL на класс — без атрибута [Collection].
/// </summary>
public abstract class ContainerApiTestBase : ApiTestBase, IClassFixture<ContainerFixture>
{
    /// <summary>
    /// Создаёт новый экземпляр <see cref="ContainerApiTestBase"/>.
    /// </summary>
    /// <param name="fixture">Запущенный контейнер с СУБД.</param>
    protected ContainerApiTestBase(ContainerFixture fixture) : base(fixture) { }
}
