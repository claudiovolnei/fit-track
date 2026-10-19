namespace FitTrack.Application.Commands;

/// <summary>Intention: replace the name and the prescription of one workout of a series.</summary>
public sealed record ChangeSeriesWorkoutCommand(
    Guid SeriesId,
    Guid WorkoutId,
    string Name,
    IReadOnlyList<PrescribedExerciseInput> Exercises);
