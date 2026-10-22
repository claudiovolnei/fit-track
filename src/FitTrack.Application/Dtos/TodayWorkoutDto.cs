namespace FitTrack.Application.Dtos;

/// <summary>
/// State of the workout the athlete already started today from a plan option. It carries the
/// elapsed seconds <em>and</em> the timestamps: with both, the client ticks locally and a skewed
/// browser clock cannot invent a duration.
/// </summary>
public sealed record TodayWorkoutDto(
    Guid WorkoutSessionId,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    int ElapsedSeconds,
    bool IsRunning,
    IReadOnlyList<PerformedSetDto> Sets)
{
    /// <summary>
    /// The exercises the athlete ticked as done. While the workout runs the screen keeps the marks
    /// on its own: the server only learns them when the workout is finished.
    /// </summary>
    public IReadOnlyList<Guid> CompletedExerciseIds { get; init; } = [];

    /// <summary>
    /// How intense the athlete said the workout was, as the word the contract names it. Null while
    /// the workout runs.
    /// </summary>
    public string? Intensity { get; init; }

    /// <summary>What the athlete wrote about the workout. Null when they wrote nothing.</summary>
    public string? Notes { get; init; }
}
