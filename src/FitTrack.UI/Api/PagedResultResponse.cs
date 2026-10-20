namespace FitTrack.UI.Api;

/// <summary>Wire contract: one page of a collection plus the metadata of the pagination control.</summary>
public sealed record PagedResultResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPrevious,
    bool HasNext);
