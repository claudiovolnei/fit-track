namespace FitTrack.Application.Commands;

/// <summary>
/// Intention: register a set the athlete just performed in the workout in progress. The repetitions are
/// not part of the intention: they come from the prescription the trainer wrote.
/// </summary>
public sealed record LogPerformedSetCommand(
    Guid WorkoutSessionId,
    Guid ExerciseId,
    decimal LoadKilograms);
