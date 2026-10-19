namespace FitTrack.Integration.Contracts;

/// <summary>Workout session returned by the API, with the clock the athlete measured.</summary>
public sealed record WorkoutSessionResponse(
    Guid Id,
    DateOnly WorkoutDate,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    int DurationSeconds,
    Guid? WorkoutSeriesWorkoutId,
    IReadOnlyList<PerformedSetResponse> Sets);
