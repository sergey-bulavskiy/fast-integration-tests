namespace FastIntegrationTests.Tests.Infrastructure.Base;

/// <summary>
/// Источник данных для повторных прогонов тестов.
/// Количество прогонов задаётся переменной окружения <c>TEST_REPEAT</c> (по умолчанию 1).
/// </summary>
public static class TestRepeat
{
    /// <summary>
    /// Возвращает <see cref="TheoryData{T}"/> с числами от 1 до <c>TEST_REPEAT</c>.
    /// Используется как <c>[MemberData(nameof(TestRepeat.Data), MemberType = typeof(TestRepeat))]</c>.
    /// </summary>
    public static TheoryData<int> Data
    {
        get
        {
            var count = int.TryParse(Environment.GetEnvironmentVariable("TEST_REPEAT"), out var n) && n > 0 ? n : 1;
            return new TheoryData<int>(Enumerable.Range(1, count));
        }
    }
}
