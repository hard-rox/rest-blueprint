using Asp.Versioning;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace RestBlueprint.Api.Extensions;

/// <summary>
/// Registers API versioning and OpenAPI services (including Scalar interactive docs).
/// </summary>
/// <remarks>
/// <b>How to add a new API version:</b>
/// <list type="number">
///   <item>Increment <see cref="ApiVersions.Current"/> or add a new constant.</item>
///   <item>Chain <c>.HasApiVersion(new ApiVersion(...))</c> in <see cref="ApiVersions.GetVersionedRouteGroup"/>.</item>
///   <item>Add <c>services.AddOpenApi("v{N}", OpenApiOptions)</c> below.</item>
///   <item>Add the new document to Scalar in <c>Program.cs</c>.</item>
/// </list>
/// </remarks>
internal static class OpenApiAndVersioningExtension
{
    /// <summary>Adds API versioning and OpenAPI document generation services.</summary>
    public static IServiceCollection AddApiVersioningServices(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(ApiVersions.Current);
                options.ReportApiVersions = true;
                options.AssumeDefaultVersionWhenUnspecified = true;
                // Clients can specify version via URL segment (/api/v1/...) or request header (x-api-version: 1).
                options.ApiVersionReader = ApiVersionReader.Combine(
                    new UrlSegmentApiVersionReader(),
                    new HeaderApiVersionReader("x-api-version"));
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'V";
                options.SubstituteApiVersionInUrl = true;
            });

        // Register one OpenAPI document per version.
        // TEMPLATE NOTE: When you add a v2, duplicate this line for "v2".
        services.AddOpenApi("v1", OpenApiOptions);

        return services;

        // ---------------------------------------------------------------------------
        // Local helper: shared schema + document transforms applied to every version.
        // ---------------------------------------------------------------------------
        static void OpenApiOptions(OpenApiOptions opt)
        {
            opt.AddSchemaTransformer((schema, context, _) =>
            {
                // Ensure numeric types map to the correct JSON Schema types.
                // Without these, ASP.NET Core OpenAPI sometimes generates "string" for numbers.
                if (context.JsonTypeInfo.Type == typeof(int) || context.JsonTypeInfo.Type == typeof(int?))
                {
                    schema.Type = JsonSchemaType.Integer;
                    schema.Pattern = null;
                }

                if (context.JsonTypeInfo.Type == typeof(long) || context.JsonTypeInfo.Type == typeof(long?))
                    schema.Type = JsonSchemaType.Number;

                if (context.JsonTypeInfo.Type == typeof(double) || context.JsonTypeInfo.Type == typeof(double?) ||
                    context.JsonTypeInfo.Type == typeof(decimal) || context.JsonTypeInfo.Type == typeof(decimal?))
                    schema.Type = JsonSchemaType.Number;

                if (context.JsonTypeInfo.Type == typeof(DateTime) || context.JsonTypeInfo.Type == typeof(DateTime?))
                    schema.Type = JsonSchemaType.String;

                return Task.CompletedTask;
            });
        }
    }

    /// <summary>
    /// Registers the Scalar interactive API reference UI and wires it to the Bearer token flow.
    /// </summary>
    public static WebApplication MapScalarDocs(this WebApplication app)
    {
        app.MapScalarApiReference(options =>
        {
            options.Title = "REST Blueprint API";

            // Bearer authentication — the developer can paste a token obtained from POST /dev/token.
            // TEMPLATE NOTE: To add an OAuth2 code flow (e.g. Keycloak), replace this with
            //   .AddAuthorizationCodeFlow(...) as shown in the SmartEnergy Billing API.
            options.AddPreferredSecuritySchemes("Bearer");
        }).AllowAnonymous();

        return app;
    }
}
