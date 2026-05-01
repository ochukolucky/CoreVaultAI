namespace CoreVault.SharedKernel.Common;

/// <summary>
/// Consistent pagination wrapper for all list endpoints
/// across all services. Every paginated response looks the same.
/// </summary>
public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    private PagedResult(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    public static PagedResult<T> Create(IReadOnlyList<T> items,
        int page, int pageSize, int totalCount) =>
        new(items, page, pageSize, totalCount);
}