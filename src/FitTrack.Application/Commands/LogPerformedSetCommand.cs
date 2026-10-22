namespace FitTrack.Application.Commands;

/// <summary>
/// Intention: register a set the athlete just performed in the workout in progress. The repetitions
/// are optional: when the athlete leaves them out, the ones the trainer prescribed are used; when the
/// athlete fills them in, the set is recorded with what was really done.
/// </summary>
public sealed record LogPerformedSetCommand(
    Guid WorkoutSessionId,
    Guid ExerciseId,
    decimal LoadKilograms,
    int? Repetitions = null);
