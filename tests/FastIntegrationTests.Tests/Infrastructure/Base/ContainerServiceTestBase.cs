namespace FastIntegrationTests.Tests.Infrastructure.Base;

/// <summary>
/// Базовый класс для интеграционных тестов сервисного уровня через Testcontainers.
/// Объявляет <see cref="IClassFixture{ContainerFixture}"/>, поэтому наследники автоматически
/// получают отдельный контейнер PostgreSQL на класс — без атрибута [Collection].
/// </summary>
public abstract class ContainerServiceTestBase : ServiceTestBase, IClassFixture<ContainerFixture>
{
    /// <summary>
    /// Создаёт новый экземпляр <see cref="ContainerServiceTestBase"/>.
    /// </summary>
    /// <param name="fixture">Запущенный контейнер с СУБД.</param>
    protected ContainerServiceTestBase(ContainerFixture fixture) : base(fixture) { }
}
