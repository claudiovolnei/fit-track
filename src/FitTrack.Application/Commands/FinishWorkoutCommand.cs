using FitTrack.Domain.WorkoutExecution;

namespace FitTrack.Application.Commands;

/// <summary>
/// Intention: stop the timer and close the workout the athlete was doing. The whole workout comes
/// at once: the exercises the athlete marked, the effort of each one, the level they gave the
/// session and what they wrote about it.
/// </summary>
public sealed record FinishWorkoutCommand(
    Guid WorkoutSessionId,
    WorkoutIntensity? Intensity,
    string? Notes,
    IReadOnlyList<PerformedExerciseInput> Exercises);
