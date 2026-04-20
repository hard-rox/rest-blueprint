namespace RestBlueprint.Api.Models.Entities;

/// <summary>
/// Represents a published article — the primary resource in this template.
/// </summary>
/// <remarks>
/// TEMPLATE NOTE: This is an in-memory record used by <see cref="DataStore"/>.
/// To persist to a database:
/// <list type="number">
///   <item>Add EF Core and a DbContext (e.g. <c>AppDbContext</c> with a <c>DbSet&lt;Article&gt;</c>).</item>
///   <item>Replace <c>DataStore.Articles</c> references in endpoints with injected DbContext queries.</item>
///   <item>Remove <c>with { ... }</c> mutations and use EF Core tracked entity updates instead.</item>
/// </list>
/// </remarks>
public sealed record Article(
    Guid Id,
    string Title,
    string Body,
    string Author,
    ArticleStatus Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
