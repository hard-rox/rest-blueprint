using FluentValidation;

namespace RestBlueprint.Api.Filters;

/// <summary>
/// An <see cref="IEndpointFilter"/> that validates the first argument of type <typeparamref name="T"/>
/// using a registered <see cref="IValidator{T}"/>.
/// </summary>
/// <typeparam name="T">The request type to validate.</typeparam>
/// <remarks>
/// The filter short-circuits the pipeline with <c>400 Bad Request</c> + grouped validation errors
/// when validation fails.  Apply it via the <c>.WithValidation&lt;T&gt;()</c> extension method
/// (see <see cref="ValidationExtensions"/>).
///
/// <b>Registration:</b> Validators are discovered automatically by
/// <c>builder.Services.AddValidatorsFromAssemblyContaining&lt;Program&gt;()</c> in <c>Program.cs</c>.
/// </remarks>
internal sealed class ValidationFilter<T>(IValidator<T>? validator = null) : IEndpointFilter
    where T : class
{
    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (validator is null)
            return await next(context);

        // Find the first argument of type T in the handler signature.
        T? argument = context.Arguments.OfType<T>().FirstOrDefault();

        if (argument is null)
            return await next(context);

        FluentValidation.Results.ValidationResult result =
            await validator.ValidateAsync(argument, context.HttpContext.RequestAborted);

        if (result.IsValid)
            return await next(context);

        // Return a structured validation problem matching the Problem Details pattern.
        Dictionary<string, string[]> errors = result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        return Results.ValidationProblem(errors);
    }
}
