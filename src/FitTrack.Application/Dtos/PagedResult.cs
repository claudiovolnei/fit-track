namespace FitTrack.Application.Dtos;

/// <summary>One page of a read model plus the metadata the pagination control needs.</summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages =>
        PageSize <= 0
            ? TotalCount == 0 ? 0 : 1
            : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPrevious => PageSize > 0 && Page > 1;

    public bool HasNext => PageSize > 0 && Page < TotalPages;
}
