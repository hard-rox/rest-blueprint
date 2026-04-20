using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace RestBlueprint.Api.Services;

// =============================================================================
// TEMPLATE NOTE — Dev Token Service
// =============================================================================
// This service generates a short-lived JWT signed with the symmetric key from
// appsettings.Development.json so that developers can obtain a bearer token
// instantly to explore authenticated endpoints via Scalar.
//
// This endpoint is only registered in Development environment (see Program.cs).
// It does NOT exist in Staging or Production.
//
// HOW TO USE:
//   1. Run the API in Development mode.
//   2. POST to /dev/token with body: { "username": "dev", "roles": ["admin"] }
//   3. Copy the returned token.
//   4. In Scalar, click "Authorize" and paste the token.
//   5. Make requests to authenticated endpoints.
//
// SECURITY:
//   • Never deploy this endpoint outside Development.
//   • The signing key in appsettings.Development.json is intentionally weak — it is
//     only for local exploration and must never be reused in any deployed environment.
// =============================================================================

/// <summary>Generates JWT tokens for local development exploration. Development-only.</summary>
internal static class DevTokenService
{
    /// <summary>
    /// Creates a signed JWT with the supplied claims, using the symmetric key from configuration.
    /// </summary>
    public static string GenerateToken(
        string username,
        IEnumerable<string> roles,
        IConfiguration configuration)
    {
        string secretKey = configuration["Authentication:SecretKey"]
                           ?? throw new InvalidOperationException("Authentication:SecretKey not configured.");
        string issuer = configuration["Authentication:Issuer"] ?? "RestBlueprint";
        string audience = configuration["Authentication:Audience"] ?? "rest-blueprint-clients";

        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(secretKey));
        SigningCredentials credentials = new(key, SecurityAlgorithms.HmacSha256);

        List<Claim> claims =
        [
            new Claim(ClaimTypes.Name, username),
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            .. roles.Select(r => new Claim(ClaimTypes.Role, r))
        ];

        JwtSecurityToken token = new(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
