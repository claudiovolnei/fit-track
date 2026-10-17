namespace FitTrack.Application.Dtos;

/// <summary>Read model of a workout session for the edge, with the clock the athlete measured.</summary>
public sealed record WorkoutSessionDto(
    Guid Id,
    DateOnly WorkoutDate,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    int DurationSeconds,
    Guid? WorkoutPlanOptionId,
    IReadOnlyList<PerformedSetDto> Sets);
