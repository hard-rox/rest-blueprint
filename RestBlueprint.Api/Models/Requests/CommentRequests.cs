namespace RestBlueprint.Api.Models.Requests;

/// <summary>Request body for creating a new comment on an article.</summary>
public sealed record CreateCommentRequest(
    string Body,
    string Author);

/// <summary>Request body for a full replacement (PUT) of an existing comment.</summary>
public sealed record UpdateCommentRequest(string Body);

/// <summary>
/// Request body for a partial update (PATCH) of an existing comment.
/// Only non-null fields are applied.
/// </summary>
public sealed record PatchCommentRequest(string? Body = null);
