using RestBlueprint.Api.Services;

namespace RestBlueprint.Api.Endpoints;

// =============================================================================
// TEMPLATE NOTE — Development Token Endpoint
// =============================================================================
// This endpoint is ONLY registered when ASPNETCORE_ENVIRONMENT=Development.
// It allows developers to obtain a bearer token instantly without setting up
// a full identity provider.
//
// HOW TO GET A TOKEN:
//   POST /dev/token
//   Content-Type: application/json
//
//   { "username": "dev-user", "roles": ["admin"] }
//
// AVAILABLE ROLES (defined in AuthExtension.cs):
//   • "admin"  — grants the "Admin" policy
//   • Any string — grants the "Authenticated" and "ReadOnly" policies
//
// USING THE TOKEN:
//   Copy the returned token.  In Scalar, click "Authorize" → paste token.
//   Alternatively: add header  Authorization: Bearer <token>
// =============================================================================

/// <summary>
/// Development-only endpoint for obtaining a JWT bearer token.
/// Registered in <c>Program.cs</c> only when the environment is Development.
/// </summary>
/// <remarks>
/// This class intentionally does NOT implement <see cref="IEndpoint"/> — it is
/// registered directly in <c>Program.cs</c> inside an
/// <c>if (app.Environment.IsDevelopment())</c> guard so the route never appears
/// in non-Development builds.
/// </remarks>
public static class DevTokenEndpoints
{
    /// <summary>Registers the <c>POST /dev/token</c> route on the application.</summary>
    public static void MapDevTokenEndpoint(this WebApplication app)
    {
        app.MapPost("/dev/token",
                (DevTokenRequest request, IConfiguration configuration) =>
                {
                    if (string.IsNullOrWhiteSpace(request.Username))
                        return Results.BadRequest(new { message = "Username is required." });

                    string token = DevTokenService.GenerateToken(
                        request.Username,
                        request.Roles ?? [],
                        configuration);

                    return Results.Ok(new DevTokenResponse(
                        token,
                        "Bearer",
                        ExpiresIn: 3600,
                        GrantedRoles: request.Roles ?? []));
                })
            .AllowAnonymous()
            .WithTags("Dev Tools")
            .WithName("GenerateDevToken")
            .WithSummary("[DEV ONLY] Generate JWT token")
            .WithDescription(
                "Development-only endpoint. Generates a signed JWT for exploring authenticated endpoints. " +
                "NOT available in Staging or Production. " +
                "Pass username and roles in the body. Valid roles: 'admin', or any string for Authenticated policy.");
    }

    // -------------------------------------------------------------------------
    // Request / Response models  (scoped to this file — no need to share them)
    // -------------------------------------------------------------------------

    /// <summary>Request body for <c>POST /dev/token</c>.</summary>
    private sealed record DevTokenRequest(
        string Username,
        IList<string>? Roles = null);

    /// <summary>Response body from <c>POST /dev/token</c>.</summary>
    private sealed record DevTokenResponse(
        string AccessToken,
        string TokenType,
        int ExpiresIn,
        IList<string> GrantedRoles);
}
