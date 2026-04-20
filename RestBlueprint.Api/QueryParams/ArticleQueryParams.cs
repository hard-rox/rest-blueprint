using RestBlueprint.Api.Models.Entities;

namespace RestBlueprint.Api.QueryParams;

/// <summary>
/// Query parameters for the <c>GET /articles</c> list endpoint.
/// </summary>
/// <remarks>
/// Use <c>[AsParameters]</c> on the handler parameter (not on this record) so that
/// ASP.NET Core binds each property from the query string individually.
///
/// Example handler signature:
/// <code>
/// group.MapGet("/", ([AsParameters] ArticleQueryParams query, CancellationToken ct) => ...)
/// </code>
/// </remarks>
public sealed record ArticleQueryParams
{
    /// <summary>Full-text search filter applied to <c>Title</c> and <c>Author</c>.</summary>
    public string? Search { get; init; }

    /// <summary>Filter by article status.</summary>
    public ArticleStatus? Status { get; init; }

    /// <summary>1-based page number. Defaults to 1.</summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>Number of items per page. Defaults to 20.</summary>
    public int PageSize { get; init; } = 20;
}
