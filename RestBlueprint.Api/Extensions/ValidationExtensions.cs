using FluentValidation;
using RestBlueprint.Api.Filters;

namespace RestBlueprint.Api.Extensions;

/// <summary>
/// Extension methods for adding the <see cref="ValidationFilter{T}"/> to Minimal API endpoint handlers.
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// Adds the <see cref="ValidationFilter{T}"/> to the endpoint, which validates the first
    /// <typeparamref name="T"/> argument in the handler using the registered <see cref="IValidator{T}"/>.
    /// </summary>
    /// <typeparam name="T">The request type to validate.</typeparam>
    /// <param name="builder">The route handler builder.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <example>
    /// <code>
    /// group.MapPost("/", (CreateArticleRequest req, IOutputCacheStore cache, CancellationToken ct) => ...)
    ///      .WithValidation&lt;CreateArticleRequest&gt;()
    ///      .RequireAuthorization("Authenticated");
    /// </code>
    /// </example>
    public static RouteHandlerBuilder WithValidation<T>(this RouteHandlerBuilder builder)
        where T : class
    {
        builder.AddEndpointFilter<ValidationFilter<T>>();
        builder.ProducesValidationProblem();
        return builder;
    }
}
