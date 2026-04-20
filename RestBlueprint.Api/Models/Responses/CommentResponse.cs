using RestBlueprint.Api.Models.Entities;

namespace RestBlueprint.Api.Models.Responses;

/// <summary>Response DTO for a comment, returned by list and detail endpoints.</summary>
public sealed record CommentResponse(
    Guid Id,
    Guid ArticleId,
    string Body,
    string Author,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    /// <summary>Maps a <see cref="Comment"/> entity to a <see cref="CommentResponse"/>.</summary>
    public static CommentResponse FromComment(Comment comment) => new(
        comment.Id,
        comment.ArticleId,
        comment.Body,
        comment.Author,
        comment.CreatedAt,
        comment.UpdatedAt);
}
