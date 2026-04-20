namespace RestBlueprint.Api.Models.Responses;

/// <summary>
/// Generic paged result wrapper returned by all list endpoints.
/// </summary>
/// <typeparam name="T">The item type in the page.</typeparam>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize)
{
    /// <summary>Total number of pages given the current page size.</summary>
    public int TotalPages => PageSize > 0
        ? (int)Math.Ceiling(TotalCount / (double)PageSize)
        : 0;

    /// <summary>True when there is a previous page available.</summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>True when there is a next page available.</summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>Creates a <see cref="PagedResult{T}"/> by applying paging to a sequence in memory.</summary>
    public static PagedResult<T> Create(IEnumerable<T> source, int pageNumber, int pageSize)
    {
        List<T> allItems = source.ToList();
        int totalCount = allItems.Count;
        List<T> paged = allItems
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<T>(paged, totalCount, pageNumber, pageSize);
    }
}
