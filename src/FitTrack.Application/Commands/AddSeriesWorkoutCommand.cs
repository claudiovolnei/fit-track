namespace FitTrack.Application.Commands;

/// <summary>Intention: add a specific workout, with its prescribed exercises, to a series.</summary>
public sealed record AddSeriesWorkoutCommand(
    Guid SeriesId,
    string Name,
    IReadOnlyList<PrescribedExerciseInput> Exercises);
