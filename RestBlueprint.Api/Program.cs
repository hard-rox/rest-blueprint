using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using RestBlueprint.Api;
using RestBlueprint.Api.Endpoints;
using RestBlueprint.Api.ExceptionHandlers;
using RestBlueprint.Api.Extensions;
using Serilog;

// =============================================================================
// REST Blueprint — Program.cs
// =============================================================================
// Service registration order matters.  Follow this sequence:
//   0. Serilog (before WebApplicationBuilder so startup errors are captured)
//   1. ProblemDetails
//   2. Exception handlers  (most-specific first, global last)
//   3. API versioning + OpenAPI
//   4. Output caching
//   5. Rate limiting
//   6. Authentication + Authorization
//   7. JSON options
//   8. CORS
//   9. FluentValidation validators
//  10. (Optional) your own services  e.g. builder.Services.AddScoped<IArticleService, ArticleService>()
//
// Middleware pipeline order (app.*) is equally important — see below.
// =============================================================================

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// ─── 0. Serilog ──────────────────────────────────────────────────────────────
// Replaces the default Microsoft logging pipeline with Serilog.
// Configuration is read from appsettings.json "Serilog" section, with a
// console sink always active.
// TEMPLATE NOTE: Add sinks (File, Seq, OpenTelemetry) here or in appsettings.json.
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console());

// ─── 1. Problem Details ──────────────────────────────────────────────────────
// Adds ProblemDetails support (RFC 7807 / RFC 9457) and injects the request
// trace ID into every problem response for easy log correlation.
builder.Services.AddProblemDetails(configure =>
{
    configure.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions.TryAdd("requestId", context.HttpContext.TraceIdentifier);
    };
});

// ─── 2. Exception handlers ───────────────────────────────────────────────────
// Register most-specific handlers first; GlobalExceptionHandler is the catch-all.
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// ─── 3. API versioning + OpenAPI ─────────────────────────────────────────────
builder.Services.AddApiVersioningServices();

// ─── 4. Output caching ───────────────────────────────────────────────────────
// Uses Redis if "ConnectionStrings:redis" is set; falls back to in-memory.
builder.Services.AddOutputCaching(builder.Configuration);

// ─── 5. Rate limiting ────────────────────────────────────────────────────────
// Global per-IP fixed-window: 100 requests per minute.
builder.Services.AddRateLimiting();

// ─── 6. Authentication + Authorization ───────────────────────────────────────
// Symmetric JWT by default.  To switch to Keycloak or ASP.NET Identity, see
// Extensions/AuthExtension.cs for step-by-step swap instructions.
builder.Services.AddBlueprintAuth(builder.Configuration);

// ─── 7. JSON options ─────────────────────────────────────────────────────────
// CamelCase property names + string enum serialization.
// TEMPLATE NOTE: Adjust as needed.  These settings propagate to all Minimal API
// responses automatically via ConfigureHttpJsonOptions.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

// ─── 8. CORS ─────────────────────────────────────────────────────────────────
builder.Services.AddCors();

// ─── 9. FluentValidation ─────────────────────────────────────────────────────
// Discovers all validators in this assembly automatically.
// TEMPLATE NOTE: If you split validators into a separate project, change the
// assembly reference accordingly.
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// ─── 10. (Placeholder) Your own services ─────────────────────────────────────
// Uncomment and wire once you are ready to move logic out of endpoint handlers:
// builder.Services.AddScoped<IArticleService, ArticleService>();

// =============================================================================
// Build the application
// =============================================================================
WebApplication app = builder.Build();

// ─── Middleware pipeline ──────────────────────────────────────────────────────
// Order is critical:
//   UseExceptionHandler   → catches all unhandled exceptions first
//   UseCors               → must be before auth and routing
//   UseAuthentication     → validates the bearer token
//   UseAuthorization      → enforces policies
//   UseRateLimiter        → limits request rate after auth (so limits are per-user-aware)
//   UseOutputCache        → caches after rate limiting

app.UseCors(options =>
{
    if (app.Environment.IsDevelopment())
    {
        // Allow any origin in development for easy local frontend testing.
        options.SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    }
    else
    {
        // In production, restrict to origins listed in Cors:AllowedOrigins.
        string[] allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];
        options.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    }
});

app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();
app.UseOutputCache();

// ─── OpenAPI document endpoints ───────────────────────────────────────────────
// Served at /openapi/v1.json — consumed by Scalar below.
app.MapOpenApi().AllowAnonymous();

// ─── Versioned application endpoints ─────────────────────────────────────────
// All IEndpoint implementations are discovered and registered here.
app.GetVersionedRouteGroup()
    .MapApplicationEndpoints();

// ─── SSE endpoint ─────────────────────────────────────────────────────────────
// The SSE endpoint is registered outside the versioned group because SSE clients
// typically maintain a persistent connection and versioning it adds friction.
// Move inside the versioned group if you need version-scoped event streams.
// NOTE: SseEndpoints.MapEndpoints() is called by MapApplicationEndpoints() above —
// it routes directly via routeBuilder.MapGet("/events/stream", ...) which attaches
// to the versioned group's parent (the WebApplication).
// SseEndpoints already calls routeBuilder.MapGet so it is handled by the loop above.

// ─── Dev token endpoint  (Development only) ──────────────────────────────────
if (app.Environment.IsDevelopment())
{
    // POST /dev/token — returns a signed JWT for exploring authenticated endpoints.
    // See Endpoints/DevTokenEndpoints.cs and Services/DevTokenService.cs.
    app.MapDevTokenEndpoint();
}

// ─── Scalar interactive API docs ─────────────────────────────────────────────
// Available at /scalar/v1
// TEMPLATE NOTE: Add more documents when you add API versions — see
//   Extensions/OpenApiAndVersioningExtension.cs → MapScalarDocs().
app.MapScalarDocs();
app.Map("/", () => Results.Redirect("/scalar")).AllowAnonymous();

app.Run();
