using RestBlueprint.Api.Models.Entities;

namespace RestBlueprint.Api.Models.Responses;

/// <summary>Response DTO for an article, returned by list and detail endpoints.</summary>
public sealed record ArticleResponse(
    Guid Id,
    string Title,
    string Body,
    string Author,
    ArticleStatus Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    /// <summary>Maps an <see cref="Article"/> entity to an <see cref="ArticleResponse"/>.</summary>
    public static ArticleResponse FromArticle(Article article) => new(
        article.Id,
        article.Title,
        article.Body,
        article.Author,
        article.Status,
        article.CreatedAt,
        article.UpdatedAt);
}
