namespace FastIntegrationTests.Application.Exceptions;

/// <summary>
/// Выбрасывается при нарушении ограничения уникальности поля сущности.
/// </summary>
public class DuplicateValueException : Exception
{
    /// <summary>
    /// Создаёт новый экземпляр <see cref="DuplicateValueException"/>.
    /// </summary>
    /// <param name="entityName">Название типа сущности.</param>
    /// <param name="fieldName">Название поля с нарушенной уникальностью.</param>
    /// <param name="value">Повторяющееся значение.</param>
    public DuplicateValueException(string entityName, string fieldName, string value)
        : base($"{entityName} с {fieldName} '{value}' уже существует.")
    {
    }
}
