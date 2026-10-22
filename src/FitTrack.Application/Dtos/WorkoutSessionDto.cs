using FitTrack.Domain.WorkoutExecution;

namespace FitTrack.Application.Dtos;

/// <summary>Read model of a workout session for the edge, with the clock the athlete measured.</summary>
public sealed record WorkoutSessionDto(
    Guid Id,
    DateOnly WorkoutDate,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    int DurationSeconds,
    Guid? WorkoutSeriesWorkoutId,
    IReadOnlyList<PerformedSetDto> Sets,
    WorkoutVolumeDto? Volume = null)
{
    /// <summary>The exercises the athlete ticked as done in this workout.</summary>
    public IReadOnlyList<Guid> CompletedExerciseIds { get; init; } = [];

    /// <summary>
    /// How intense the athlete said the workout was, as the word the contract names it. Null for the
    /// sessions finished before the athlete started answering it.
    /// </summary>
    public string? Intensity { get; init; }

    /// <summary>What the athlete wrote about the workout. Null when they wrote nothing.</summary>
    public string? Notes { get; init; }

    /// <summary>
    /// The series the workout came from, at the moment of the read. Null for a session logged free
    /// or whose prescription no longer exists — no snapshot of the name is kept.
    /// </summary>
    public string? WorkoutSeriesName { get; init; }
}
