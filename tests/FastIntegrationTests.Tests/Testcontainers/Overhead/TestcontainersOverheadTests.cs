namespace FastIntegrationTests.Tests.Testcontainers.Overhead;

/// <summary>
/// Пустые тесты для замера накладных расходов инфраструктуры: Testcontainers (миграция на каждый тест).
/// Тело теста не делает ничего — измеряется только InitializeAsync/DisposeAsync.
/// Количество прогонов: TEST_REPEAT (по умолчанию 1).
/// </summary>
public class TestcontainersOverheadTests : ContainerServiceTestBase
{
    public TestcontainersOverheadTests(ContainerFixture fixture) : base(fixture) { }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public Task Noop(int _) => Task.CompletedTask;
}
