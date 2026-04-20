namespace FastIntegrationTests.Tests.Respawn.Overhead;

/// <summary>
/// Пустые тесты для замера накладных расходов инфраструктуры: Respawn (миграция один раз, сброс данных ~1 мс).
/// Тело теста не делает ничего — измеряется только InitializeAsync/DisposeAsync.
/// Количество прогонов: TEST_REPEAT (по умолчанию 1).
/// </summary>
public class RespawnOverheadTests : RespawnServiceTestBase
{
    public RespawnOverheadTests(RespawnFixture fixture) : base(fixture) { }

    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public Task Noop(int _) => Task.CompletedTask;
}
