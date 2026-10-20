namespace FitTrack.Application.Dtos;

/// <summary>
/// One page of the workout history. The summary describes every session the filter matched, so the
/// statistics cards do not change when the athlete walks to the next page.
/// </summary>
public sealed record WorkoutHistoryDto(
    IReadOnlyList<WorkoutSessionDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    WorkoutVolumeSummaryDto? Summary)
{
    public int TotalPages =>
        PageSize <= 0
            ? TotalCount == 0 ? 0 : 1
            : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPrevious => PageSize > 0 && Page > 1;

    public bool HasNext => PageSize > 0 && Page < TotalPages;
}
