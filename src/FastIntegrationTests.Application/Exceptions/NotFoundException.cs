namespace FastIntegrationTests.Application.Exceptions;

/// <summary>
/// Выбрасывается, когда запрашиваемая сущность не найдена в базе данных.
/// </summary>
public class NotFoundException : Exception
{
    /// <summary>
    /// Создаёт новый экземпляр <see cref="NotFoundException"/>.
    /// </summary>
    /// <param name="entityName">Название типа сущности.</param>
    /// <param name="id">Идентификатор сущности.</param>
    public NotFoundException(string entityName, object id)
        : base($"{entityName} с идентификатором '{id}' не найден.")
    {
    }
}
