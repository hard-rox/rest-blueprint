using RestBlueprint.Api.Extensions;
using RestBlueprint.Api.Models.Entities;
using RestBlueprint.Api.Models.Requests;
using RestBlueprint.Api.Models.Responses;
using RestBlueprint.Api.QueryParams;

namespace RestBlueprint.Api.Endpoints;

// =============================================================================
// TEMPLATE NOTE — Article Endpoints
// =============================================================================
// This file demonstrates the full REST lifecycle for a resource (Articles).
//
// All handlers are INLINE — they access DataStore directly.
// To switch to a service layer:
//   1. Create IArticleService / ArticleService in Services/ (stubs provided).
//   2. Register: builder.Services.AddScoped<IArticleService, ArticleService>()  in Program.cs.
//   3. Add the service as a handler parameter: (ArticleQueryParams q, IArticleService svc, ...) => ...
//   4. Replace all DataStore.Articles.* calls with await svc.ListAsync(q, ct) etc.
//
// Cache tags:
//   • All GET endpoints tag responses with "articles".
//   • All mutations (POST/PUT/PATCH/DELETE) evict the "articles" tag.
// =============================================================================

/// <summary>
/// REST endpoints for the <c>articles</c> resource.
/// Demonstrates GET (list + detail), POST, PUT, PATCH, and DELETE.
/// </summary>
public sealed class ArticleEndpoints : IEndpoint
{
    private const string CacheTag = "articles";

    public void MapEndpoints(IEndpointRouteBuilder routeBuilder)
    {
        RouteGroupBuilder group = routeBuilder.MapGroup("articles")
            .WithName("Articles")
            .WithTags("articles")
            .WithSummary("Operations on articles")
            .WithDescription("Create, read, update, and delete articles.");

        // -------------------------------------------------------------------------
        // GET /articles  — paged list with optional search & status filter
        // -------------------------------------------------------------------------
        group.MapGet("/",
                ([AsParameters] ArticleQueryParams query, CancellationToken ct) =>
                {
                    IEnumerable<Article> filtered = DataStore.Articles.Values;

                    // Apply optional filters.
                    if (!string.IsNullOrWhiteSpace(query.Search))
                        filtered = filtered.Where(a =>
                            a.Title.Contains(query.Search, StringComparison.OrdinalIgnoreCase) ||
                            a.Author.Contains(query.Search, StringComparison.OrdinalIgnoreCase));

                    if (query.Status.HasValue)
                        filtered = filtered.Where(a => a.Status == query.Status.Value);

                    IEnumerable<ArticleResponse> responses = filtered
                        .OrderByDescending(a => a.CreatedAt)
                        .Select(ArticleResponse.FromArticle);

                    PagedResult<ArticleResponse> result = PagedResult<ArticleResponse>.Create(
                        responses, query.PageNumber, query.PageSize);

                    return Results.Ok(result);
                })
            .AllowAnonymous()
            .WithName("ListArticles")
            .WithSummary("List articles")
            .WithDescription("Returns a paged, optionally filtered list of articles.")
            .Produces<PagedResult<ArticleResponse>>()
            .Produces(StatusCodes.Status500InternalServerError)
            .CacheOutput(p => p.Tag(CacheTag));

        // -------------------------------------------------------------------------
        // GET /articles/{id}  — single article by ID
        // -------------------------------------------------------------------------
        group.MapGet("/{id:guid}",
                (Guid id) =>
                {
                    return DataStore.Articles.TryGetValue(id, out Article? article)
                        ? Results.Ok(ArticleResponse.FromArticle(article))
                        : Results.NotFound(new { message = $"Article '{id}' was not found." });
                })
            .AllowAnonymous()
            .WithName("GetArticleById")
            .WithSummary("Get article")
            .WithDescription("Returns a single article by its identifier.")
            .Produces<ArticleResponse>()
            .Produces<object>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError)
            .CacheOutput(p => p.Tag(CacheTag));

        // -------------------------------------------------------------------------
        // POST /articles  — create a new article  (authenticated)
        // -------------------------------------------------------------------------
        group.MapPost("/",
                (CreateArticleRequest request, IOutputCacheStore cache, CancellationToken ct) =>
                {
                    Article article = new(
                        Id: Guid.NewGuid(),
                        Title: request.Title,
                        Body: request.Body,
                        Author: request.Author,
                        Status: request.Status,
                        CreatedAt: DateTime.UtcNow,
                        UpdatedAt: null);

                    DataStore.Articles[article.Id] = article;
                    cache.EvictByTagAsync(CacheTag, ct);

                    return Results.Created($"/api/v1/articles/{article.Id}",
                        ArticleResponse.FromArticle(article));
                })
            .RequireAuthorization("Authenticated")
            .WithValidation<CreateArticleRequest>()
            .WithName("CreateArticle")
            .WithSummary("Create article")
            .WithDescription("Creates a new article. Requires authentication.")
            .Produces<ArticleResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);

        // -------------------------------------------------------------------------
        // PUT /articles/{id}  — full replacement  (authenticated)
        // PUT replaces ALL mutable fields. Fields absent from the body receive defaults.
        // -------------------------------------------------------------------------
        group.MapPut("/{id:guid}",
                (Guid id, UpdateArticleRequest request, IOutputCacheStore cache, CancellationToken ct) =>
                {
                    if (!DataStore.Articles.TryGetValue(id, out Article? existing))
                        return Results.NotFound(new { message = $"Article '{id}' was not found." });

                    Article updated = existing with
                    {
                        Title = request.Title,
                        Body = request.Body,
                        Status = request.Status,
                        UpdatedAt = DateTime.UtcNow
                    };

                    DataStore.Articles[id] = updated;
                    cache.EvictByTagAsync(CacheTag, ct);

                    return Results.Ok(ArticleResponse.FromArticle(updated));
                })
            .RequireAuthorization("Authenticated")
            .WithValidation<UpdateArticleRequest>()
            .WithName("ReplaceArticle")
            .WithSummary("Replace article (PUT)")
            .WithDescription("Fully replaces an article. All mutable fields must be supplied.")
            .Produces<ArticleResponse>()
            .Produces<object>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);

        // -------------------------------------------------------------------------
        // PATCH /articles/{id}  — partial update  (authenticated)
        // Only supplied (non-null) fields are applied to the existing resource.
        // -------------------------------------------------------------------------
        group.MapPatch("/{id:guid}",
                (Guid id, PatchArticleRequest request, IOutputCacheStore cache, CancellationToken ct) =>
                {
                    if (!DataStore.Articles.TryGetValue(id, out Article? existing))
                        return Results.NotFound(new { message = $"Article '{id}' was not found." });

                    Article patched = existing with
                    {
                        Title = request.Title ?? existing.Title,
                        Body = request.Body ?? existing.Body,
                        Status = request.Status ?? existing.Status,
                        UpdatedAt = DateTime.UtcNow
                    };

                    DataStore.Articles[id] = patched;
                    cache.EvictByTagAsync(CacheTag, ct);

                    return Results.Ok(ArticleResponse.FromArticle(patched));
                })
            .RequireAuthorization("Authenticated")
            .WithValidation<PatchArticleRequest>()
            .WithName("PatchArticle")
            .WithSummary("Patch article (PATCH)")
            .WithDescription("Partially updates an article. Only supplied (non-null) fields are modified.")
            .Produces<ArticleResponse>()
            .Produces<object>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);

        // -------------------------------------------------------------------------
        // DELETE /articles/{id}  — remove article  (authenticated)
        // -------------------------------------------------------------------------
        group.MapDelete("/{id:guid}",
                (Guid id, IOutputCacheStore cache, CancellationToken ct) =>
                {
                    if (!DataStore.Articles.TryRemove(id, out _))
                        return Results.NotFound(new { message = $"Article '{id}' was not found." });

                    // Also remove all comments for this article.
                    foreach (Guid commentId in DataStore.Comments.Values
                        .Where(c => c.ArticleId == id)
                        .Select(c => c.Id)
                        .ToList())
                    {
                        DataStore.Comments.TryRemove(commentId, out _);
                    }

                    cache.EvictByTagAsync(CacheTag, ct);
                    cache.EvictByTagAsync("comments", ct);

                    return Results.NoContent();
                })
            .RequireAuthorization("Authenticated")
            .WithName("DeleteArticle")
            .WithSummary("Delete article")
            .WithDescription("Deletes an article and all its comments. Returns 204 No Content on success.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<object>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}
