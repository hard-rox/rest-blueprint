namespace RestBlueprint.Api.Models.Entities;

/// <summary>
/// Represents a comment on an <see cref="Article"/> — the sub-resource in this template.
/// </summary>
/// <remarks>
/// TEMPLATE NOTE: Replace <see cref="DataStore.Comments"/> access in endpoints with an
/// EF Core <c>DbSet&lt;Comment&gt;</c> once you add persistence.
/// </remarks>
public sealed record Comment(
    Guid Id,
    Guid ArticleId,
    string Body,
    string Author,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
