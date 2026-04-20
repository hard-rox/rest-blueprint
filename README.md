# REST Blueprint

A production-ready **.NET 10 Minimal API** template demonstrating REST best practices in a single
standalone project — ready to clone, rename, and evolve into a real API.

## What's Included

| Feature | Implementation |
|---|---|
| Minimal API structure | `IEndpoint` pattern with reflection-based auto-discovery |
| API versioning | URL segment (`/api/v1/...`) + header (`x-api-version`) via `Asp.Versioning` |
| Interactive API docs | Scalar UI at `/scalar/v1`, OpenAPI JSON at `/openapi/v1.json` |
| JWT authentication | Symmetric HMAC-SHA256 bearer tokens; dev token endpoint in Development |
| Authorization policies | `"Authenticated"`, `"Admin"`, `"ReadOnly"` |
| Input validation | FluentValidation + `WithValidation<T>()` endpoint filter |
| Output caching | Tag-based eviction; Redis when configured, in-memory fallback |
| Rate limiting | Per-IP fixed-window: 100 req/min |
| Server-Sent Events | Heartbeat stream at `GET /events/stream` |
| Problem Details | RFC 9457 errors with `requestId` on every response |
| Structured logging | Serilog with console sink; configurable via `appsettings.json` |
| CORS | Permissive in Development; origin-locked in Production |

---

## Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### Run locally

```bash
git clone https://github.com/your-org/rest-blueprint.git
cd rest-blueprint
dotnet run --project RestBlueprint.Api
```

Open **http://localhost:5000/scalar/v1** to explore the API interactively.

### Get a dev token

```bash
curl -s -X POST http://localhost:5000/dev/token \
  -H "Content-Type: application/json" \
  -d '{"username":"dev","roles":["admin"]}' | jq .accessToken
```

Paste the token into Scalar's **Bearer** field and you're ready to call authenticated endpoints.

---

## Template Repository Structure

```
rest-blueprint/                          ← template source repo
├── .github/
│   ├── copilot-instructions.md          ← Copilot instructions
│   ├── skills/rest-minimal-api/         ← Skill file loaded by Copilot agents
│   └── workflows/                       ← CI/CD (not scaffolded)
├── .template.config/template.json       ← dotnet new template manifest
└── RestBlueprint.Api/                   ← scaffolded as {Name}.Api.*
    ├── Program.cs
    ├── IEndpoint.cs
    ├── ApiVersions.cs
    ├── DataStore.cs
    ├── Endpoints/
    ├── Extensions/
    ├── ExceptionHandlers/
    ├── Filters/
    ├── Models/
    ├── QueryParams/
    ├── Services/
    └── Validators/
```

## Scaffolded Project Structure

When you run `dotnet new rest-blueprint -n MyApi`, you get:

```
MyApi/
├── MyApi.Api.csproj                     ← renamed from RestBlueprint.Api.csproj
├── Program.cs                           ← composition root: DI + middleware pipeline
├── IEndpoint.cs                         ← marker interface auto-discovered at startup
├── ApiVersions.cs                       ← versioned route group builder
├── DataStore.cs                         ← in-memory store (replace with EF Core DbContext)
├── Endpoints/
│   ├── ArticleEndpoints.cs              ← full CRUD for /articles
│   ├── CommentEndpoints.cs              ← sub-resource /articles/{id}/comments
│   ├── SseEndpoints.cs                  ← GET /events/stream (SSE heartbeat)
│   └── DevTokenEndpoints.cs             ← POST /dev/token [Development only]
├── Extensions/
│   ├── AuthExtension.cs                 ← JWT Bearer + policies
│   ├── EndpointExtension.cs             ← reflection-based endpoint discovery
│   ├── OpenApiAndVersioningExtension.cs ← Scalar + versioning wiring
│   ├── OutputCacheExtension.cs          ← Redis / in-memory cache setup
│   ├── RateLimitExtension.cs            ← per-IP rate limiter
│   └── ValidationExtensions.cs          ← WithValidation<T>() helper
├── ExceptionHandlers/                   ← Problem Details for validation + uncaught errors
├── Filters/                             ← ValidationFilter<T>
├── Models/
│   ├── Entities/                        ← Article, Comment, ArticleStatus
│   ├── Requests/                        ← Create/Update/Patch request records
│   └── Responses/                       ← Response DTOs + PagedResult<T>
├── QueryParams/                         ← [AsParameters] query records
├── Services/                            ← IArticleService stub + DevTokenService
└── Validators/                          ← FluentValidation validators
```

> **Tip — adding to an existing solution:** When adding via an IDE's "New Project" dialog, the project files scaffold directly into the output directory you choose (e.g. `MySolution/MyApi/`), with no extra root-level files. Add the generated `.csproj` to your solution with `dotnet sln add MyApi/MyApi.Api.csproj`.

---

## Adding a New Resource

1. `Models/Entities/YourEntity.cs` — sealed record entity
2. `Models/Requests/YourEntityRequests.cs` — Create / Update / Patch records
3. `Models/Responses/YourEntityResponse.cs` — response DTO with `FromEntity()` factory
4. `QueryParams/YourEntityQueryParams.cs` — `[AsParameters]` record for list filters
5. `Validators/YourEntityRequestValidators.cs` — FluentValidation classes
6. `DataStore.cs` — add `ConcurrentDictionary<Guid, YourEntity>` + seed rows
7. `Endpoints/YourEntityEndpoints.cs` — implement `IEndpoint`; no registration needed

The group builder pattern every endpoint file must follow:

```csharp
RouteGroupBuilder group = routeBuilder.MapGroup("your-entities")
    .WithName("YourEntities")
    .WithTags("your-entities")
    .WithSummary("Operations on your-entities")
    .WithDescription("Create, read, update, and delete your-entities.");
```

> **Never** use `.WithGroupName()` — it overrides the API-version group assignment and removes all
> endpoints from the Scalar/OpenAPI document.

---

## Configuration

All settings live in `appsettings.json` and can be overridden per environment or via environment
variables.

| Key | Default | Purpose |
|---|---|---|
| `Authentication:SecretKey` | placeholder | HMAC-SHA256 signing key — **change before deploying** |
| `Authentication:Issuer` | `RestBlueprint` | JWT issuer claim |
| `Authentication:Audience` | `rest-blueprint-clients` | JWT audience claim |
| `ConnectionStrings:redis` | _(empty)_ | Redis connection string; empty = in-memory cache |
| `Cors:AllowedOrigins` | example URL | Allowed origins in Production |
| `Serilog` | _(not set)_ | Full Serilog sink + level configuration |

### Example production Serilog configuration (`appsettings.json`)

```json
"Serilog": {
  "MinimumLevel": {
    "Default": "Information",
    "Override": {
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "WriteTo": [
    { "Name": "Console" },
    { "Name": "File", "Args": { "path": "logs/api-.log", "rollingInterval": "Day" } }
  ]
}
```

---

## Evolving This Template into a Real API

Work through this checklist when hardening the template for production:

- [ ] Replace `DataStore` with an EF Core `DbContext`
- [ ] Connect a real database (SQLite for local, PostgreSQL / MySQL for production)
- [ ] Swap JWT auth to Keycloak or ASP.NET Core Identity (see `AuthExtension.cs`)
- [ ] Set a strong `Authentication:SecretKey` via secrets manager / environment variable
- [ ] Set `ConnectionStrings:redis` for production output caching
- [ ] Lock `Cors:AllowedOrigins` to your actual front-end origins
- [ ] Remove or guard `POST /dev/token` — it must never reach production
- [ ] Add production Serilog sinks (Seq, OpenTelemetry, File)
- [ ] Add a test project (xUnit + `WebApplicationFactory` for integration tests)
- [ ] Plan API versioning lifecycle (deprecate v1 when v2 is stable)
- [ ] Remove `DataStore.cs` once persistence is in place

---

## License

MIT — see [LICENSE](LICENSE).
