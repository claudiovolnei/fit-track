namespace FitTrack.Application.Dtos;

/// <summary>
/// Criteria shared by every collection read. <see cref="PageSize"/> zero means "the whole filtered
/// collection", which is how the HTTP edge keeps answering a plain array for callers that send no
/// pagination parameters.
/// </summary>
public sealed record ListQuery
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    public const int MaxSearchLength = 80;

    public static readonly ListQuery All = new(null, 1, 0);

    private ListQuery(string? search, int page, int pageSize)
    {
        Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        Page = page;
        PageSize = pageSize;
    }

    /// <summary>The term the adapter matches with "contains", or null when there is no filter.</summary>
    public string? Search { get; }

    public int Page { get; }

    /// <summary>Zero means the collection is not paginated.</summary>
    public int PageSize { get; }

    public bool IsPaged => PageSize > 0;

    public int Skip => IsPaged ? (Page - 1) * PageSize : 0;

    public int Take => PageSize;

    public static ListQuery Paged(string? search, int page, int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, MaxPageSize);

        return new ListQuery(search, page, pageSize);
    }

    public static ListQuery Filtered(string? search) => new(search, 1, 0);
}
