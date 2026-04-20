---
name: rest-minimal-api
description: 'REST design and Minimal API implementation workflow for REST Blueprint. Use when creating endpoints, adding resources/sub-resources, implementing auth, caching, validation, SSE, or modifying the API structure.'
argument-hint: 'Use for REST endpoint design, IEndpoint pattern, caching, auth policies, FluentValidation, SSE, or template customization.'
---

# REST + Minimal API Skill

Use this skill for all backend work in this template (REST Blueprint).

---

## Core Workflow

1. Identify the affected layer: models, validators, endpoints, or infrastructure (extensions/handlers).
2. Start from existing endpoint files before introducing a new pattern.
3. Follow REST URL conventions (see below) before naming routes.
4. Wire validation with `WithValidation<T>()` for every mutation endpoint.
5. Tag every GET endpoint with `CacheOutput(p => p.Tag("..."))`.
6. Evict cache tags in POST / PUT / PATCH / DELETE handlers.
7. Apply correct authorization policy (see auth section).
8. Add full OpenAPI metadata: `.WithName()`, `.WithSummary()`, `.WithDescription()`, `.Produces<T>()`, `.ProducesProblem()`.

---

## REST URL Conventions

| Rule | Good | Bad |
|---|---|---|
| Plural resource nouns | `/articles` | `/article`, `/getArticles` |
| Kebab-case for multi-word | `/article-tags` | `/articleTags`, `/article_tags` |
| Nest sub-resources at most ONE level | `/articles/{id}/comments` | `/articles/{id}/comments/{cid}/replies/{rid}` |
| If deeper nesting needed, prefer flat route | `/replies/{replyId}` | `/articles/{id}/comments/{cid}/replies` |
| Version in URL path | `/api/v1/articles` | `/articles?version=1` |
| No trailing slash for single resource | `/articles/{id}` | `/articles/{id}/` |
| Collection path for list | `/articles` | `/articles/list` |

---

## HTTP Method Semantics & Status Codes

| Method | Semantics | Success Code | Notes |
|---|---|---|---|
| GET | Fetch resource(s) — idempotent, safe | 200 OK | 404 if not found |
| POST | Create a new resource | 201 Created + `Location` header | Return created resource in body |
| PUT | Full replacement of a resource | 200 OK | All mutable fields required |
| PATCH | Partial update — only supplied fields | 200 OK | Null fields = no change |
| DELETE | Remove a resource | 204 No Content | 404 if not found |

Additional status codes:
- `400 Bad Request` — validation failure (Problem Details with `errors` extension)
- `401 Unauthorized` — no or invalid JWT
- `403 Forbidden` — authenticated but insufficient role
- `429 Too Many Requests` — rate limit exceeded
- `500 Internal Server Error` — unhandled exception (Problem Details)

---

## IEndpoint Pattern

Every endpoint group lives in a class implementing `IEndpoint` in `Endpoints/`.

```csharp
// Endpoints/ProductEndpoints.cs
public sealed class ProductEndpoints : IEndpoint
{
    private const string CacheTag = "products";

    public void MapEndpoints(IEndpointRouteBuilder routeBuilder)
    {
        RouteGroupBuilder group = routeBuilder.MapGroup("products")
            .WithName("Products")
            .WithTags("products")
            .WithSummary("Operations on products")
            .WithDescription("Create, read, update, and delete products.");

        group.MapGet("/", (CancellationToken ct) => Results.Ok(DataStore.Products.Values))
            .AllowAnonymous()
            .WithName("ListProducts")
            .WithSummary("List products")
            .Produces<IEnumerable<ProductResponse>>()
            .CacheOutput(p => p.Tag(CacheTag));
    }
}
```

**Group builder rules:**
- `.WithName("Products")` — unique route group name (used by link generation).
- `.WithTags("products")` — **lowercase**, matches the OpenAPI tag; used for visual grouping in Scalar.
- **Do NOT use `.WithGroupName()`** — it overrides the API-version group name and removes all endpoints in the group from the OpenAPI document.
- `.WithSummary()` / `.WithDescription()` on the group appear in Scalar's tag description.

**No manual registration is needed.** `EndpointExtension.MapApplicationEndpoints()` discovers all
`IEndpoint` classes via reflection at startup.

---

## Logging (Serilog)

The template uses **Serilog** (via `Serilog.AspNetCore`) as the logging backend, replacing the
default Microsoft logging pipeline. It is wired in `Program.cs` **before** any service registration
so that startup errors are also captured:

```csharp
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console());
```

`ReadFrom.Configuration` reads the `"Serilog"` section from `appsettings.json`, so sinks, minimum
levels, and overrides can be configured without recompiling.

**Do NOT inject `ILogger<T>` into endpoint handler lambdas.** Serilog enriches and writes
all Microsoft.Extensions.Logging calls, so framework-level events (requests, auth, caching) are
logged automatically. Add handler-level logging only in services, not inline handlers.

To add more sinks (File, Seq, OpenTelemetry), chain them in `Program.cs` or configure them in
`appsettings.json` using the `Serilog.Sinks.*` packages.

---

## Output Caching

### Tag a GET response
```csharp
group.MapGet("/", handler)
    .CacheOutput(p => p.Tag("products"));
```

### Evict on mutation
```csharp
group.MapPost("/",
    async (CreateProductRequest req, IOutputCacheStore cache, CancellationToken ct) =>
    {
        // ... create logic ...
        await cache.EvictByTagAsync("products", ct);
        return Results.Created(...);
    });
```

**Rules:**
- Every GET that returns a collection or a cacheable detail should have a tag.
- Every POST / PUT / PATCH / DELETE that modifies a resource must evict the tag(s) for that resource and any parent resources.
- Inject `IOutputCacheStore` as a handler parameter to evict.
- Do NOT cache user-scoped responses (per-user data should include a vary-by-user policy or skip caching).

---

## Rate Limiting

The global per-IP limiter (100 req/min) is applied automatically.  To apply a named limiter to a specific endpoint:

```csharp
// 1. Add a named limiter in RateLimitExtension.cs:
options.AddFixedWindowLimiter("strict", opt => { opt.PermitLimit = 10; opt.Window = TimeSpan.FromMinutes(1); });

// 2. Apply to endpoint:
group.MapPost("/expensive-operation", handler)
    .RequireRateLimiting("strict");
```

---

## Authentication & Authorization

### Policies defined in `AuthExtension.cs`

| Policy name | Requirement |
|---|---|
| `"Authenticated"` | Any valid JWT — no role required |
| `"Admin"` | Valid JWT + `admin` role claim |
| `"ReadOnly"` | Any valid JWT (semantic alias for read-only operations) |

### Apply to an endpoint
```csharp
.RequireAuthorization("Admin")       // Admin only
.RequireAuthorization("Authenticated") // Any logged-in user
.AllowAnonymous()                    // Public endpoint
```

Default policy (set in `AuthExtension.cs`): `Authenticated` — endpoints without explicit auth
settings require a valid JWT.  Always add `.AllowAnonymous()` to public GET endpoints.

### Swapping auth mechanism
- **Keycloak**: See the multi-line comment at the top of `Extensions/AuthExtension.cs`.
- **ASP.NET Core Identity**: See the comment block in the same file.
- **Custom JWT provider**: Replace only the `AddJwtBearer(...)` block; keep the policies.

---

## FluentValidation + `WithValidation<T>()`

### Write a validator (in `Validators/`)
```csharp
internal sealed class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Price).GreaterThan(0);
    }
}
```
Validators are discovered automatically by `AddValidatorsFromAssemblyContaining<Program>()`.

### Apply to an endpoint
```csharp
group.MapPost("/", handler)
    .WithValidation<CreateProductRequest>();
```
This adds `ValidationFilter<T>` which resolves `IValidator<T>` from DI and short-circuits with
`400 ValidationProblem` on failure.  `.ProducesValidationProblem()` is added automatically.

### PATCH validators — validate only supplied fields
```csharp
internal sealed class PatchProductRequestValidator : AbstractValidator<PatchProductRequest>
{
    public PatchProductRequestValidator()
    {
        When(x => x.Name is not null, () =>
            RuleFor(x => x.Name!).NotEmpty().MaximumLength(100));
    }
}
```

---

## Problem Details

Use the appropriate `Results.*` factory based on the error type:

| Scenario | Use |
|---|---|
| Resource not found | `Results.NotFound(new { message = "..." })` |
| Validation failure | Handled automatically by `ValidationFilter<T>` / `ValidationExceptionHandler` |
| Business rule violation | `Results.Problem(detail: "...", statusCode: 400)` |
| Unauthorized | `Results.Unauthorized()` |
| Forbidden | `Results.Forbid()` |
| Unhandled exception | Handled automatically by `GlobalExceptionHandler` |

Every Problem Details response automatically includes `requestId` from the trace identifier
(configured in `Program.cs` via `AddProblemDetails`).

---

## SSE (Server-Sent Events)

### Wire format
```
id: 1
event: article-published
data: {"id":"...","title":"..."}

```
(Blank line terminates each event block.)

### Minimal implementation
```csharp
app.MapGet("/events/stream", async (HttpContext http, CancellationToken ct) =>
{
    http.Response.Headers["Content-Type"]  = "text/event-stream";
    http.Response.Headers["Cache-Control"] = "no-cache";
    await http.Response.Body.FlushAsync(ct);

    await foreach (var evt in _channel.Reader.ReadAllAsync(ct))
    {
        string block = $"id: {evt.Id}\nevent: {evt.Type}\ndata: {JsonSerializer.Serialize(evt)}\n\n";
        await http.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(block), ct);
        await http.Response.Body.FlushAsync(ct);
    }
});
```

See `Endpoints/SseEndpoints.cs` for the full working heartbeat example.

---

## How to Add a New Resource (Step-by-Step)

1. **Entity** — add `Models/Entities/YourEntity.cs` (sealed record).
2. **Request models** — add `Models/Requests/YourEntityRequests.cs` with Create/Update/Patch records.
3. **Response model** — add `Models/Responses/YourEntityResponse.cs` with a `FromEntity()` factory.
4. **Query params** — add `QueryParams/YourEntityQueryParams.cs` with `[AsParameters]` if the list endpoint needs filtering.
5. **Validators** — add `Validators/YourEntityRequestValidators.cs` with inline validators.
6. **Data** — add a `ConcurrentDictionary<Guid, YourEntity>` in `DataStore.cs` with seed data.
7. **Endpoint** — add `Endpoints/YourEntityEndpoints.cs` implementing `IEndpoint`.
8. **Cache tag** — choose a tag name (e.g. `"your-entities"`) and apply it to GET + evict in mutations.
9. **Auth** — decide which policy each endpoint requires.
10. **No registration needed** — `MapApplicationEndpoints()` discovers the new class automatically.

---

## Common Pitfalls

| Pitfall | Fix |
|---|---|
| GET endpoint not cached | Add `.CacheOutput(p => p.Tag("..."))` |
| Mutation doesn't evict cache | Inject `IOutputCacheStore` and call `await cache.EvictByTagAsync(tag, ct)` |
| POST returns 200 instead of 201 | Use `Results.Created(location, body)` |
| DELETE returns 200 instead of 204 | Use `Results.NoContent()` |
| Missing `.AllowAnonymous()` on public GET | The default policy requires auth — always be explicit |
| Validation not triggered | Check `.WithValidation<T>()` is chained on the handler builder |
| `AsParameters` not binding | Ensure `[AsParameters]` is on the record, NOT the handler parameter |
| PATCH applying null as a value | Check `?? existing.Field` in the `with { }` expression |
| JWT clock skew causing 401 | The 5-minute `ClockSkew` in `AuthExtension.cs` handles minor drift |
| `.WithGroupName()` on route group | **Never use** — overrides the API-version group name and hides all endpoints in the group from the Scalar/OpenAPI document. Use `.WithName()` instead. |
| SSE buffered by reverse proxy | Set `X-Accel-Buffering: no` (nginx) and `X-Buffering: no` (Caddy) |
