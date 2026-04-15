using Microsoft.AspNetCore.Diagnostics;

namespace FastIntegrationTests.WebApi.Middleware;

/// <summary>
/// Глобальный обработчик исключений.
/// Преобразует доменные исключения в HTTP-ответы с соответствующими статус-кодами.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    /// <inheritdoc/>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken ct)
    {
        (int statusCode, string message) = exception switch
        {
            NotFoundException ex => (StatusCodes.Status404NotFound, ex.Message),
            InvalidOrderStatusTransitionException ex => (StatusCodes.Status400BadRequest, ex.Message),
            _ => (0, string.Empty),
        };

        if (statusCode == 0)
            return false;

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new { error = message }, ct);
        return true;
    }
}
