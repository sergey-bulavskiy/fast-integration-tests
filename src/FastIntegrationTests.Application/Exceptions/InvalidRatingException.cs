namespace FastIntegrationTests.Application.Exceptions;

/// <summary>
/// Выбрасывается при указании рейтинга отзыва вне диапазона 1–5.
/// </summary>
public class InvalidRatingException : Exception
{
    /// <summary>
    /// Создаёт новый экземпляр <see cref="InvalidRatingException"/>.
    /// </summary>
    /// <param name="rating">Недопустимый рейтинг.</param>
    public InvalidRatingException(int rating)
        : base($"Рейтинг '{rating}' недопустим. Допустимы значения от 1 до 5.")
    {
    }
}
