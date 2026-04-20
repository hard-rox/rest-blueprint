using RestBlueprint.Api.Models.Entities;

namespace RestBlueprint.Api.Models.Requests;

/// <summary>Request body for creating a new article.</summary>
public sealed record CreateArticleRequest(
    string Title,
    string Body,
    string Author,
    ArticleStatus Status = ArticleStatus.Draft);

/// <summary>Request body for a full replacement (PUT) of an existing article.</summary>
/// <remarks>
/// PUT replaces the entire resource.  Fields not supplied receive their default values.
/// Use <see cref="PatchArticleRequest"/> for partial updates.
/// </remarks>
public sealed record UpdateArticleRequest(
    string Title,
    string Body,
    ArticleStatus Status);

/// <summary>
/// Request body for a partial update (PATCH) of an existing article.
/// Only non-null fields are applied; null fields are left unchanged.
/// </summary>
public sealed record PatchArticleRequest(
    string? Title = null,
    string? Body = null,
    ArticleStatus? Status = null);
