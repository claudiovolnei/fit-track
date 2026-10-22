using FitTrack.Domain.WorkoutCatalog;

namespace FitTrack.Application.Commands;

/// <summary>
/// What the athlete did of one prescribed exercise: the repetitions and the load they really used.
/// The number of sets comes from the prescription — the athlete adjusts the effort, not the sheet.
/// </summary>
public sealed record PerformedExerciseInput(
    Guid ExerciseId,
    int Repetitions,
    decimal LoadKilograms)
{
    /// <summary>The exercise the entry belongs to, in the domain's own vocabulary.</summary>
    public ExerciseId ToExerciseId() => new(ExerciseId);
}
