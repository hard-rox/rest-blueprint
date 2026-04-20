using RestBlueprint.Api.Models.Entities;
using RestBlueprint.Api.Models.Requests;
using RestBlueprint.Api.Models.Responses;
using RestBlueprint.Api.QueryParams;

namespace RestBlueprint.Api.Services;

// =============================================================================
// TEMPLATE NOTE — Service Layer (Optional Pattern)
// =============================================================================
// This interface and its implementation are STUBS provided to guide you when
// you are ready to move business logic out of endpoint handlers.
//
// The default template uses INLINE handlers that access DataStore directly —
// this keeps the code easy to read and explore in a template context.
//
// WHEN TO USE A SERVICE LAYER:
//   • When multiple endpoints share the same business logic.
//   • When you add EF Core and want to keep DbContext inside a service, not an endpoint.
//   • When you want to write unit tests against the business logic without invoking HTTP.
//
// HOW TO SWITCH:
//   1. Implement the methods below in ArticleService.cs using EF Core or your preferred store.
//   2. Register in Program.cs:
//        builder.Services.AddScoped<IArticleService, ArticleService>();
//   3. Inject into the endpoint handler:
//        group.MapGet("/", async ([AsParameters] ArticleQueryParams q, IArticleService svc, CancellationToken ct)
//            => Results.Ok(await svc.ListAsync(q, ct)));
//   4. Remove DataStore references from ArticleEndpoints.cs.
// =============================================================================

/// <summary>
/// Optional service abstraction for article CRUD operations.
/// Implement this interface to move business logic out of endpoint handlers.
/// </summary>
public interface IArticleService
{
    /// <summary>Returns a paged, filtered list of articles.</summary>
    Task<PagedResult<ArticleResponse>> ListAsync(ArticleQueryParams query, CancellationToken ct);

    /// <summary>Returns a single article or null if not found.</summary>
    Task<ArticleResponse?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>Creates a new article and returns the created resource.</summary>
    Task<ArticleResponse> CreateAsync(CreateArticleRequest request, CancellationToken ct);

    /// <summary>Fully replaces an article. Returns null if not found.</summary>
    Task<ArticleResponse?> ReplaceAsync(Guid id, UpdateArticleRequest request, CancellationToken ct);

    /// <summary>Partially updates an article. Returns null if not found.</summary>
    Task<ArticleResponse?> PatchAsync(Guid id, PatchArticleRequest request, CancellationToken ct);

    /// <summary>Deletes an article. Returns true if deleted, false if not found.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}
