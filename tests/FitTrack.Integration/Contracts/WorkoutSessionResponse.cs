namespace FitTrack.Integration.Contracts;

/// <summary>Workout session returned by the API, with the clock the athlete measured.</summary>
public sealed record WorkoutSessionResponse(
    Guid Id,
    DateOnly WorkoutDate,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    int DurationSeconds,
    Guid? WorkoutSeriesWorkoutId,
    IReadOnlyList<PerformedSetResponse> Sets)
{
    /// <summary>Null when the session has no workout reference to compare against.</summary>
    public WorkoutVolumeResponse? Volume { get; init; }

    /// <summary>The exercises the athlete ticked as done in this workout.</summary>
    public IReadOnlyList<Guid> CompletedExerciseIds { get; init; } = [];

    /// <summary>
    /// How intense the athlete said the workout was. Null for the sessions finished before the athlete
    /// started answering it.
    /// </summary>
    public string? Intensity { get; init; }

    /// <summary>What the athlete wrote about the workout. Null when they wrote nothing.</summary>
    public string? Notes { get; init; }
}
