using RestBlueprint.Api.Extensions;
using RestBlueprint.Api.Models.Entities;
using RestBlueprint.Api.Models.Requests;
using RestBlueprint.Api.Models.Responses;

namespace RestBlueprint.Api.Endpoints;

// =============================================================================
// TEMPLATE NOTE — Comment Endpoints (Sub-Resource Pattern)
// =============================================================================
// Comments are a sub-resource of Articles and are nested accordingly:
//   GET    /articles/{articleId}/comments
//   GET    /articles/{articleId}/comments/{commentId}
//   POST   /articles/{articleId}/comments
//   PUT    /articles/{articleId}/comments/{commentId}
//   PATCH  /articles/{articleId}/comments/{commentId}
//   DELETE /articles/{articleId}/comments/{commentId}
//
// REST nesting rule: limit to ONE level of nesting for clarity.
// If comments ever need their own sub-resources, prefer a flat route instead
// (e.g. GET /comments/{commentId}/replies) rather than triple-nesting.
//
// Cache tags:
//   • GET endpoints tag with "comments".
//   • Mutations evict "comments".
// =============================================================================

/// <summary>
/// REST endpoints for the <c>comments</c> sub-resource, nested under <c>articles</c>.
/// Demonstrates the sub-resource pattern with full CRUD.
/// </summary>
public sealed class CommentEndpoints : IEndpoint
{
    private const string CacheTag = "comments";

    public void MapEndpoints(IEndpointRouteBuilder routeBuilder)
    {
        // Nest under articles — the {articleId} is a route constraint shared by all handlers below.
        RouteGroupBuilder group = routeBuilder.MapGroup("articles/{articleId:guid}/comments")
            .WithName("Comments")
            .WithTags("comments")
            .WithSummary("Operations on comments")
            .WithDescription("Create, read, update, and delete comments on articles.");

        // -------------------------------------------------------------------------
        // GET /articles/{articleId}/comments  — list comments for an article
        // -------------------------------------------------------------------------
        group.MapGet("/",
                (Guid articleId, CancellationToken ct) =>
                {
                    if (!DataStore.Articles.ContainsKey(articleId))
                        return Results.NotFound(new { message = $"Article '{articleId}' was not found." });

                    IEnumerable<CommentResponse> comments = DataStore.Comments.Values
                        .Where(c => c.ArticleId == articleId)
                        .OrderBy(c => c.CreatedAt)
                        .Select(CommentResponse.FromComment);

                    return Results.Ok(comments);
                })
            .AllowAnonymous()
            .WithName("ListComments")
            .WithSummary("List comments")
            .WithDescription("Returns all comments for a specific article.")
            .Produces<IEnumerable<CommentResponse>>()
            .Produces<object>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError)
            .CacheOutput(p => p.Tag(CacheTag));

        // -------------------------------------------------------------------------
        // GET /articles/{articleId}/comments/{commentId}
        // -------------------------------------------------------------------------
        group.MapGet("/{commentId:guid}",
                (Guid articleId, Guid commentId) =>
                {
                    if (!DataStore.Articles.ContainsKey(articleId))
                        return Results.NotFound(new { message = $"Article '{articleId}' was not found." });

                    return DataStore.Comments.TryGetValue(commentId, out Comment? comment) && comment.ArticleId == articleId
                        ? Results.Ok(CommentResponse.FromComment(comment))
                        : Results.NotFound(new { message = $"Comment '{commentId}' was not found." });
                })
            .AllowAnonymous()
            .WithName("GetCommentById")
            .WithSummary("Get comment")
            .WithDescription("Returns a single comment by its identifier.")
            .Produces<CommentResponse>()
            .Produces<object>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError)
            .CacheOutput(p => p.Tag(CacheTag));

        // -------------------------------------------------------------------------
        // POST /articles/{articleId}/comments  — create a comment  (authenticated)
        // -------------------------------------------------------------------------
        group.MapPost("/",
                (Guid articleId, CreateCommentRequest request, IOutputCacheStore cache, CancellationToken ct) =>
                {
                    if (!DataStore.Articles.ContainsKey(articleId))
                        return Results.NotFound(new { message = $"Article '{articleId}' was not found." });

                    Comment comment = new(
                        Id: Guid.NewGuid(),
                        ArticleId: articleId,
                        Body: request.Body,
                        Author: request.Author,
                        CreatedAt: DateTime.UtcNow,
                        UpdatedAt: null);

                    DataStore.Comments[comment.Id] = comment;
                    cache.EvictByTagAsync(CacheTag, ct);

                    return Results.Created(
                        $"/api/v1/articles/{articleId}/comments/{comment.Id}",
                        CommentResponse.FromComment(comment));
                })
            .RequireAuthorization("Authenticated")
            .WithValidation<CreateCommentRequest>()
            .WithName("CreateComment")
            .WithSummary("Create comment")
            .WithDescription("Adds a comment to an article. Requires authentication.")
            .Produces<CommentResponse>(StatusCodes.Status201Created)
            .Produces<object>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);

        // -------------------------------------------------------------------------
        // PUT /articles/{articleId}/comments/{commentId}  — full replace  (authenticated)
        // -------------------------------------------------------------------------
        group.MapPut("/{commentId:guid}",
                (Guid articleId, Guid commentId, UpdateCommentRequest request,
                    IOutputCacheStore cache, CancellationToken ct) =>
                {
                    if (!DataStore.Articles.ContainsKey(articleId))
                        return Results.NotFound(new { message = $"Article '{articleId}' was not found." });

                    if (!DataStore.Comments.TryGetValue(commentId, out Comment? existing) ||
                        existing.ArticleId != articleId)
                        return Results.NotFound(new { message = $"Comment '{commentId}' was not found." });

                    Comment updated = existing with
                    {
                        Body = request.Body,
                        UpdatedAt = DateTime.UtcNow
                    };

                    DataStore.Comments[commentId] = updated;
                    cache.EvictByTagAsync(CacheTag, ct);

                    return Results.Ok(CommentResponse.FromComment(updated));
                })
            .RequireAuthorization("Authenticated")
            .WithValidation<UpdateCommentRequest>()
            .WithName("ReplaceComment")
            .WithSummary("Replace comment (PUT)")
            .WithDescription("Fully replaces a comment. All mutable fields must be supplied.")
            .Produces<CommentResponse>()
            .Produces<object>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);

        // -------------------------------------------------------------------------
        // PATCH /articles/{articleId}/comments/{commentId}  — partial update  (authenticated)
        // -------------------------------------------------------------------------
        group.MapPatch("/{commentId:guid}",
                (Guid articleId, Guid commentId, PatchCommentRequest request,
                    IOutputCacheStore cache, CancellationToken ct) =>
                {
                    if (!DataStore.Articles.ContainsKey(articleId))
                        return Results.NotFound(new { message = $"Article '{articleId}' was not found." });

                    if (!DataStore.Comments.TryGetValue(commentId, out Comment? existing) ||
                        existing.ArticleId != articleId)
                        return Results.NotFound(new { message = $"Comment '{commentId}' was not found." });

                    Comment patched = existing with
                    {
                        Body = request.Body ?? existing.Body,
                        UpdatedAt = DateTime.UtcNow
                    };

                    DataStore.Comments[commentId] = patched;
                    cache.EvictByTagAsync(CacheTag, ct);

                    return Results.Ok(CommentResponse.FromComment(patched));
                })
            .RequireAuthorization("Authenticated")
            .WithValidation<PatchCommentRequest>()
            .WithName("PatchComment")
            .WithSummary("Patch comment (PATCH)")
            .WithDescription("Partially updates a comment. Only supplied (non-null) fields are modified.")
            .Produces<CommentResponse>()
            .Produces<object>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);

        // -------------------------------------------------------------------------
        // DELETE /articles/{articleId}/comments/{commentId}  — remove  (authenticated)
        // -------------------------------------------------------------------------
        group.MapDelete("/{commentId:guid}",
                (Guid articleId, Guid commentId, IOutputCacheStore cache, CancellationToken ct) =>
                {
                    if (!DataStore.Articles.ContainsKey(articleId))
                        return Results.NotFound(new { message = $"Article '{articleId}' was not found." });

                    if (!DataStore.Comments.TryGetValue(commentId, out Comment? existing) ||
                        existing.ArticleId != articleId)
                        return Results.NotFound(new { message = $"Comment '{commentId}' was not found." });

                    DataStore.Comments.TryRemove(commentId, out _);
                    cache.EvictByTagAsync(CacheTag, ct);

                    return Results.NoContent();
                })
            .RequireAuthorization("Authenticated")
            .WithName("DeleteComment")
            .WithSummary("Delete comment")
            .WithDescription("Deletes a comment. Returns 204 No Content on success.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<object>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}
