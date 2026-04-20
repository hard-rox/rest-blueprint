# Contributing to REST Blueprint

Thank you for taking the time to improve this template. The goal of REST Blueprint is to stay
**focused and opinionated**: every piece of code should serve as a clear, copy-paste-ready example
of a specific REST / Minimal API pattern.

---

## Ground Rules

- Keep changes small and scoped. One PR per feature or fix.
- Do not add packages without a strong reason — the template must stay lightweight.
- Do not add application business logic. All domain code is intentionally stub-level.
- Follow the existing code style (see [Code Style](#code-style) below).
- All public-facing changes must be reflected in `README.md` and, if pattern-level,
  in `.github/skills/rest-minimal-api/SKILL.md`.

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Any editor — VS Code + C# Dev Kit or Visual Studio 2022+ recommended

### Fork and run

```bash
git clone https://github.com/your-org/rest-blueprint.git
cd rest-blueprint
dotnet build
dotnet run --project RestBlueprint.Api
```

Open **http://localhost:5000/scalar/v1** to verify everything starts cleanly.

---

## What Belongs in This Template

| In scope | Out of scope |
|---|---|
| New REST pattern examples (e.g. HATEOAS links, ETag, pagination cursors) | Full application business logic |
| Additional infrastructure patterns (e.g. health checks, OpenTelemetry) | Domain-specific models / workflows |
| Auth swap guides (Keycloak, Identity) inside comment blocks | Third-party identity provider SDKs |
| Test project demonstrating `WebApplicationFactory` usage | End-to-end / load tests |
| Bug fixes and clarifying code comments | Framework upgrades without testing |

---

## Making a Change

### 1. Create a branch

```bash
git switch -c feat/your-feature-name
# or
git switch -c fix/your-bug-description
```

Branch naming convention:
- `feat/` — new pattern or capability
- `fix/` — bug fix
- `docs/` — documentation only
- `chore/` — dependency updates, formatting

### 2. Make your changes

- Follow the [IEndpoint pattern](RestBlueprint.Api/Endpoints/ArticleEndpoints.cs) for new
  endpoints.
- Use the group builder convention:
  ```csharp
  RouteGroupBuilder group = routeBuilder.MapGroup("resource-name")
      .WithName("ResourceName")
      .WithTags("resource-name")
      .WithSummary("Operations on resource-name")
      .WithDescription("Create, read, update, and delete resource-name.");
  ```
- Add FluentValidation for every mutation endpoint via `.WithValidation<T>()`.
- Tag all GET responses with `.CacheOutput(p => p.Tag("..."))` and evict in mutations.
- Do not inject `ILogger<T>` into endpoint handler lambdas — Serilog captures framework events
  automatically.

### 3. Verify the build

```bash
dotnet build RestBlueprint.Api/RestBlueprint.Api.csproj
```

The build must produce **0 errors and 0 warnings**.

### 4. Update documentation

If your change introduces or modifies a pattern:

- Update `README.md` if it affects quick-start instructions or the feature table.
- Update `.github/skills/rest-minimal-api/SKILL.md` if it affects a coding pattern, pitfall, or
  workflow step.

### 5. Open a pull request

- Use a clear title: `feat: add health check endpoint` or `fix: evict comments cache on article delete`.
- Describe **what** changed and **why**.
- Link any related issues.

---

## Code Style

The project follows standard C# conventions with a few template-specific additions.

| Rule | Example |
|---|---|
| Sealed records for entities and DTOs | `public sealed record Article(...)` |
| Sealed classes for endpoint groups | `public sealed class ArticleEndpoints : IEndpoint` |
| `private const string CacheTag` in every endpoint class | `private const string CacheTag = "articles";` |
| Inline handlers — no named methods | `group.MapGet("/", (Guid id) => ...)` |
| Explicit `.AllowAnonymous()` on every public endpoint | Do not rely on implicit defaults |
| Structured log messages with named properties | `Log.Information("Article {ArticleId} created", id)` |
| Vertical alignment for `with { }` expressions | `Title = request.Title,` (padded) |

Formatting is handled by the .editorconfig / implicit .NET formatting rules. Run
`dotnet format` before committing if your editor does not auto-format on save.

---

## Reporting Issues

Open a GitHub Issue with:

1. A clear description of the problem or suggestion.
2. The .NET SDK version (`dotnet --version`).
3. Steps to reproduce (for bugs).
4. Expected vs actual behaviour.

---

## License

By contributing you agree that your contributions will be licensed under the
[MIT License](LICENSE) that covers this project.
