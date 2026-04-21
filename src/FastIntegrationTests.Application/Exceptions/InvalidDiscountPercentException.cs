namespace FastIntegrationTests.Application.Exceptions;

/// <summary>
/// Выбрасывается при указании процента скидки вне диапазона 1–100.
/// </summary>
public class InvalidDiscountPercentException : Exception
{
    /// <summary>
    /// Создаёт новый экземпляр <see cref="InvalidDiscountPercentException"/>.
    /// </summary>
    /// <param name="percent">Недопустимый процент.</param>
    public InvalidDiscountPercentException(int percent)
        : base($"Процент скидки '{percent}' недопустим. Допустимы значения от 1 до 100.")
    {
    }
}
