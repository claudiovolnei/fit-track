namespace FitTrack.Application.Commands;

/// <summary>
/// Intention: tick (or untick) one exercise of the workout in progress as done. The tick is what the
/// athlete uses to say the exercise of the prescription is finished, with or without a recorded load.
/// </summary>
public sealed record MarkExerciseCompletedCommand(
    Guid WorkoutSessionId,
    Guid ExerciseId,
    bool Completed);
