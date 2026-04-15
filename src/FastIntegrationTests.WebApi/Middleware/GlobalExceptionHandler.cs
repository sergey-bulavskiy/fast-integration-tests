using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace FastIntegrationTests.WebApi.Middleware;

/// <summary>
/// Глобальный обработчик исключений.
/// Преобразует доменные исключения в HTTP-ответы в формате RFC 7807 ProblemDetails.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="GlobalExceptionHandler"/>.
    /// </summary>
    /// <param name="problemDetailsService">Сервис формирования ответов ProblemDetails.</param>
    public GlobalExceptionHandler(IProblemDetailsService problemDetailsService)
    {
        _problemDetailsService = problemDetailsService;
    }

    /// <inheritdoc/>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken ct)
    {
        (int statusCode, string detail) = exception switch
        {
            NotFoundException ex => (StatusCodes.Status404NotFound, ex.Message),
            InvalidOrderStatusTransitionException ex => (StatusCodes.Status400BadRequest, ex.Message),
            _ => (0, string.Empty),
        };

        if (statusCode == 0)
            return false;

        context.Response.StatusCode = statusCode;
        await _problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Detail = detail,
            },
        });
        return true;
    }
}
