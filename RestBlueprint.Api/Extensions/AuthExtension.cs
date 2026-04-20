using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace RestBlueprint.Api.Extensions;

/// <summary>
/// Registers JWT Bearer authentication and authorization policies.
/// </summary>
/// <remarks>
/// <b>Default auth mechanism:</b> Symmetric-key JWT signed with the secret in
/// <c>Authentication:SecretKey</c>.  In Development, a valid token can be obtained at
/// <c>POST /dev/token</c> (see <c>DevTokenEndpoints.cs</c>).
///
/// <b>Swapping to Keycloak:</b>
/// <code>
///   // 1. Remove the symmetric key block below.
///   // 2. Replace with:
///   options.Authority       = $"{config["KEYCLOAK_HTTP"]}{config["Authentication:Issuer"]}";
///   options.MetadataAddress = $"{config["KEYCLOAK_INTERNAL"]}{config["Authentication:MetadataAddress"]}";
///   options.RequireHttpsMetadata = false;
///   options.TokenValidationParameters = new TokenValidationParameters
///   {
///       ValidateIssuer   = true,
///       ValidateAudience = false,   // Keycloak may omit audience
///       ValidateLifetime = true,
///       ClockSkew        = TimeSpan.FromMinutes(5)
///   };
///   // 3. Add the OnTokenValidated event to map realm_access.roles to ClaimTypes.Role
///   //    (see SmartEnergy.Billing.API/Extensions/AuthExtension.cs for the full example).
/// </code>
///
/// <b>Swapping to ASP.NET Core Identity:</b>
/// <code>
///   // 1. Add Microsoft.AspNetCore.Identity.EntityFrameworkCore + EF Core packages.
///   // 2. Add: builder.Services.AddIdentity&lt;ApplicationUser, IdentityRole&gt;()
///   //             .AddEntityFrameworkStores&lt;AppDbContext&gt;();
///   // 3. Replace JWT Bearer with cookie-based auth or the Identity token provider.
///   // 4. Update authorization policies to use Identity roles.
/// </code>
///
/// <b>Authorization policies defined here:</b>
/// <list type="bullet">
///   <item><c>"Authenticated"</c> — any authenticated user (default policy).</item>
///   <item><c>"Admin"</c>        — requires the <c>admin</c> role claim.</item>
///   <item><c>"ReadOnly"</c>     — any authenticated user (alias useful for documenting intent).</item>
/// </list>
///
/// To apply a policy to an endpoint: <c>.RequireAuthorization("Admin")</c>
/// To allow anonymous access:        <c>.AllowAnonymous()</c>
/// </remarks>
internal static class AuthExtension
{
    /// <summary>
    /// Adds JWT Bearer authentication with symmetric-key signing and defines authorization policies.
    /// </summary>
    public static IServiceCollection AddBlueprintAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string secretKey = configuration["Authentication:SecretKey"]
                            ?? throw new InvalidOperationException(
                                "Authentication:SecretKey is not configured. " +
                                "Set it in appsettings.Development.json or user secrets.");
        string issuer = configuration["Authentication:Issuer"] ?? "RestBlueprint";
        string audience = configuration["Authentication:Audience"] ?? "rest-blueprint-clients";

        SymmetricSecurityKey signingKey = new(Encoding.UTF8.GetBytes(secretKey));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false; // allow HTTP in development

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ClockSkew = TimeSpan.FromMinutes(5)
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        ILogger logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("Authentication");

                        logger.LogError(context.Exception,
                            "Authentication failed. Path: {Path}, Error: {Error}",
                            context.HttpContext.Request.Path,
                            context.Exception.Message);

                        return Task.CompletedTask;
                    },
                    OnForbidden = context =>
                    {
                        ILogger logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("Authorization");

                        logger.LogWarning("Forbidden. Path: {Path}, User: {User}",
                            context.HttpContext.Request.Path,
                            context.HttpContext.User.FindFirst(ClaimTypes.Name)?.Value ?? "anonymous");

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization(opt =>
        {
            // Default policy — all endpoints require authentication unless .AllowAnonymous() is applied.
            opt.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            // Require the authenticated user to carry the "admin" role claim.
            opt.AddPolicy("Admin", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole("admin"));

            // Any authenticated user — use this where role is irrelevant.
            opt.AddPolicy("Authenticated", policy =>
                policy.RequireAuthenticatedUser());

            // Read-only policy — semantically identical to Authenticated, but signals intent.
            // TEMPLATE NOTE: Adjust as needed (e.g. add a "reader" role claim requirement).
            opt.AddPolicy("ReadOnly", policy =>
                policy.RequireAuthenticatedUser());
        });

        return services;
    }
}
