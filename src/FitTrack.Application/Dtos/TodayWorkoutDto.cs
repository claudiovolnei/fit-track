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
    IReadOnlyList<PerformedSetDto> Sets);
