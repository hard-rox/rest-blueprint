# REST Blueprint — GitHub Copilot Instructions

## Project Overview

REST Blueprint is a production-ready **.NET 10 Minimal API** template demonstrating REST best
practices, the `IEndpoint` pattern, FluentValidation, JWT authentication, API versioning, output
caching, rate limiting, SSE, and Problem Details — all in a single standalone project.

It is designed to be **cut-and-pasted** into a new repository and evolved into a real API.

---

## ⚡ Load the Skill FIRST

Before working on any feature in this template, **read the skill file**:

```
.github/skills/rest-minimal-api/SKILL.md
```

The skill contains:
- REST URL conventions and HTTP method semantics
- `IEndpoint` pattern guide
- Caching tag strategy and eviction rules
- Auth policy table and swap guide (Keycloak / ASP.NET Identity)
- FluentValidation + `WithValidation<T>()` pattern
- Problem Details usage guide
- SSE pattern
- "How to add a new resource" — step-by-step checklist
- Common pitfalls table

---

## Directory Quick Reference

> The tree below shows the **template source repo**. Only the contents of `RestBlueprint.Api/` are scaffolded into generated projects; root-level files are for template development only.

```
rest-blueprint/
├── .github/
│   ├── copilot-instructions.md          ← this file — loaded automatically by Copilot
│   ├── skills/rest-minimal-api/         ← skill file loaded on demand
│   └── workflows/                       ← CI/CD (NOT scaffolded into generated projects)
├── .template.config/template.json       ← dotnet new template manifest
└── RestBlueprint.Api/                   ← scaffolded as {Name}.Api.*
    ├── Program.cs                       ← app composition root — middleware pipeline + DI
    ├── GlobalUsings.cs                  ← project-wide global usings
    ├── IEndpoint.cs                     ← marker interface for all endpoint groups
    ├── ApiVersions.cs                   ← versioned route group builder (/api/v{v}/...)
    ├── DataStore.cs                     ← in-memory store (replace with EF Core DbContext)
    ├── Endpoints/
    │   ├── ArticleEndpoints.cs          ← GET/POST/PUT/PATCH/DELETE /articles
    │   ├── CommentEndpoints.cs          ← sub-resource /articles/{id}/comments
    │   ├── SseEndpoints.cs              ← GET /events/stream (SSE heartbeat)
    │   └── DevTokenEndpoints.cs         ← POST /dev/token  [DEV ONLY]
    ├── Extensions/
    │   ├── AuthExtension.cs             ← JWT Bearer + policies (+ Keycloak/Identity swap guide)
    │   ├── EndpointExtension.cs         ← reflection-based endpoint discovery
    │   ├── OpenApiAndVersioningExtension.cs ← versioning, OpenAPI docs, Scalar wiring
    │   ├── OutputCacheExtension.cs      ← Redis / in-memory output cache
    │   ├── RateLimitExtension.cs        ← per-IP fixed-window rate limiter
    │   └── ValidationExtensions.cs      ← WithValidation<T>() helper
    ├── ExceptionHandlers/
    │   ├── ValidationExceptionHandler.cs ← 400 ProblemDetails for FluentValidation errors
    │   └── GlobalExceptionHandler.cs    ← 500 ProblemDetails for all other exceptions
    ├── Filters/
    │   └── ValidationFilter.cs          ← IEndpointFilter used by WithValidation<T>()
    ├── Models/
    │   ├── Entities/                    ← Article, Comment, ArticleStatus
    │   ├── Requests/                    ← Create/Update/Patch request records
    │   └── Responses/                   ← ArticleResponse, CommentResponse, PagedResult<T>
    ├── QueryParams/
    │   └── ArticleQueryParams.cs        ← [AsParameters] record for GET /articles
    ├── Services/
    │   ├── IArticleService.cs           ← optional service abstraction (stub — see note inside)
    │   ├── ArticleService.cs            ← empty stub — fill with EF Core data access
    │   └── DevTokenService.cs           ← JWT generator for POST /dev/token
    └── Validators/
        ├── ArticleRequestValidators.cs  ← Create/Update/Patch article validators
        └── CommentRequestValidators.cs  ← Create/Update/Patch comment validators
```

---

## Core Conventions

### JSON Options (`Program.cs`)
- Property naming: **camelCase**
- Enum serialization: **string** (not integer) in **camelCase**
- Null fields omitted from responses (`WhenWritingNull`)

### API Versioning
- URL segment: `/api/v{version:apiVersion}/...`
- Header fallback: `x-api-version: 1`
- Current version: `ApiVersions.Current = 1`

### Authentication
- JWT Bearer, symmetric HMAC-SHA256 key
- Dev token: `POST /dev/token` (Development only)
- Policies: `"Authenticated"`, `"Admin"`, `"ReadOnly"`
- Default: all endpoints require auth unless `.AllowAnonymous()` is explicit

### Output Caching
- Redis when `ConnectionStrings:redis` is set; in-memory fallback otherwise
- Tag pattern: one tag per resource (e.g. `"articles"`, `"comments"`)
- GET endpoints: `.CacheOutput(p => p.Tag("articles"))`
- Mutations: `await cache.EvictByTagAsync("articles", ct)`

### Rate Limiting
- Global per-IP: 100 req/min fixed window
- Returns `HTTP 429` immediately (no queue)

### Problem Details
- All errors follow RFC 9457 Problem Details format
- `requestId` included in every problem response for log correlation

### Logging
- **Serilog** (`Serilog.AspNetCore`) replaces the default Microsoft logging pipeline
- Wired in `Program.cs` as step 0 (before all service registrations): `builder.Host.UseSerilog(...)`
- Console sink always active; additional sinks/levels configurable via `"Serilog"` section in `appsettings.json`
- Do **not** inject `ILogger<T>` into endpoint handler lambdas — log only in services

---

## How to Add a New Resource

1. `Models/Entities/Product.cs` — add entity record
2. `Models/Entities/ProductStatus.cs` — add status enum (if needed)
3. `Models/Requests/ProductRequests.cs` — add Create/Update/Patch records
4. `Models/Responses/ProductResponse.cs` — add response DTO with `FromProduct()` factory
5. `QueryParams/ProductQueryParams.cs` — add `[AsParameters]` query record
6. `Validators/ProductRequestValidators.cs` — add FluentValidation classes
7. `DataStore.cs` — add `ConcurrentDictionary<Guid, Product>` + seed data
8. `Endpoints/ProductEndpoints.cs` — implement `IEndpoint`, use cache tag `"products"`
9. **No registration needed** — the endpoint is discovered automatically

---

## Template Customization Checklist

When evolving this template into a real API:

- [ ] Replace `DataStore` with an EF Core `DbContext` (see `DataStore.cs` comment block)
- [ ] Connect a real database (SQLite for local, MySQL/Postgres for production)
- [ ] Swap auth to Keycloak or ASP.NET Identity (see `AuthExtension.cs` comment block)
- [ ] Set a strong `Authentication:SecretKey` (or remove it once Keycloak is wired)
- [ ] Set `ConnectionStrings:redis` for production output caching
- [ ] Update `Cors:AllowedOrigins` in `appsettings.json` for production
- [ ] Remove or guard `POST /dev/token` — confirm it is only in Development
- [ ] Add additional Serilog sinks for production (Seq, OpenTelemetry, File) via `appsettings.json` or `Program.cs`
- [ ] Add proper API versioning lifecycle (deprecate v1 when v2 is stable)
- [ ] Add a test project (xUnit + WebApplicationFactory for integration tests)
- [ ] Remove `DataStore.cs` once persistence is in place
