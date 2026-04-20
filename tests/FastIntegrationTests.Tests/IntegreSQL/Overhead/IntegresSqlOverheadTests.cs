namespace FastIntegrationTests.Tests.IntegreSQL.Overhead;

/// <summary>
/// Пустые тесты для замера накладных расходов инфраструктуры: IntegreSQL (~5 мс клонирование БД, без миграции).
/// Тело теста не делает ничего — измеряется только InitializeAsync/DisposeAsync.
/// Количество прогонов: TEST_REPEAT (по умолчанию 1).
/// </summary>
public class IntegresSqlOverheadTests : AppServiceTestBase
{
    [Theory]
    [MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]
    public Task Noop(int _) => Task.CompletedTask;
}
