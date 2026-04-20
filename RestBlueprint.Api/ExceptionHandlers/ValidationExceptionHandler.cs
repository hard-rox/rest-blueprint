using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

namespace RestBlueprint.Api.ExceptionHandlers;

/// <summary>
/// Exception handler that intercepts <see cref="FluentValidation.ValidationException"/> and returns
/// a <c>400 Bad Request</c> Problem Details response with field-level error details.
/// </summary>
/// <remarks>
/// Registered before <see cref="GlobalExceptionHandler"/> so it runs first.
/// Returns <c>false</c> for any exception that is not a <see cref="ValidationException"/>,
/// allowing other handlers to process it.
///
/// The response body groups errors by property name:
/// <code>
/// {
///   "type": "Validation Error",
///   "title": "One or more validation errors occurred.",
///   "status": 400,
///   "extensions": {
///     "errors": [
///       { "propertyName": "Title", "errors": [{ "attemptedValue": "", "errorMessage": "..." }] }
///     ]
///   }
/// }
/// </code>
/// </remarks>
internal sealed class ValidationExceptionHandler(
    ILogger<ValidationExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception? exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validationException)
            return false;

        logger.LogTrace(validationException,
            "Validation error for {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Type = "Validation Error",
                Title = "One or more validation errors occurred.",
                Status = StatusCodes.Status400BadRequest,
                Extensions = new Dictionary<string, object?>
                {
                    {
                        "errors", validationException.Errors
                            .GroupBy(vf => vf.PropertyName)
                            .Select(g => new
                            {
                                PropertyName = g.Key,
                                Errors = g.Select(vf => new
                                {
                                    vf.AttemptedValue,
                                    vf.ErrorMessage
                                })
                            })
                    }
                }
            }
        });
    }
}
