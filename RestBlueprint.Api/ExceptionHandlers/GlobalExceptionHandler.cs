using Microsoft.AspNetCore.Diagnostics;

namespace RestBlueprint.Api.ExceptionHandlers;

/// <summary>
/// Last-resort exception handler that catches any unhandled exception and returns a
/// <c>500 Internal Server Error</c> Problem Details response.
/// </summary>
/// <remarks>
/// Registered last in the exception handler pipeline so that more-specific handlers
/// (e.g. <see cref="ValidationExceptionHandler"/>) run first and return <c>false</c>
/// to yield control here only for truly unexpected errors.
/// </remarks>
internal sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception,
            "Unhandled exception for {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                Title = "An unexpected error occurred.",
                Detail = exception.Message,
                Status = StatusCodes.Status500InternalServerError
            }
        });
    }
}
