using Asp.Versioning;
using Asp.Versioning.Builder;

namespace RestBlueprint.Api;

/// <summary>
/// Central API versioning configuration.
/// </summary>
/// <remarks>
/// <b>How to add a new version:</b>
/// <list type="number">
///   <item>Add a new <c>public const int V2 = 2;</c> constant.</item>
///   <item>Chain <c>.HasApiVersion(new ApiVersion(V2))</c> inside <see cref="GetVersionedRouteGroup"/>.</item>
///   <item>Tag endpoints with <c>.MapToApiVersion(V2)</c> as needed.</item>
///   <item>Add <c>services.AddOpenApi("v2", ...)</c> in <c>OpenApiAndVersioningExtension.cs</c>.</item>
///   <item>Add the v2 document to Scalar in <c>Program.cs</c>.</item>
/// </list>
/// To deprecate a version, call <c>.HasDeprecatedApiVersion(new ApiVersion(V1))</c>.
/// </remarks>
internal static class ApiVersions
{
    /// <summary>Current (latest) API version number.</summary>
    public const int Current = 1;

    /// <summary>
    /// Builds the versioned <see cref="RouteGroupBuilder"/> that all endpoint groups hang off.
    /// Routes will be available at <c>/api/v{version:apiVersion}/...</c>.
    /// </summary>
    public static RouteGroupBuilder GetVersionedRouteGroup(this WebApplication app)
    {
        ApiVersionSet apiVersionSet = app.NewApiVersionSet()
            // .HasDeprecatedApiVersion(new ApiVersion(1))   // uncomment to mark a version as deprecated
            .HasApiVersion(new ApiVersion(Current))
            .ReportApiVersions()
            .Build();

        RouteGroupBuilder versionedRouteGroup = app
            .MapGroup("/api/v{version:apiVersion}")
            .WithApiVersionSet(apiVersionSet);

        return versionedRouteGroup;
    }
}
