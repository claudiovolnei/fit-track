namespace FitTrack.Application.Commands;

/// <summary>Intention: register a set the athlete just performed in the workout in progress.</summary>
public sealed record LogPerformedSetCommand(
    Guid WorkoutSessionId,
    Guid ExerciseId,
    decimal LoadKilograms,
    int Repetitions);
