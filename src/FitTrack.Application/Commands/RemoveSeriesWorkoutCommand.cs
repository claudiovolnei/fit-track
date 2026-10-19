namespace FitTrack.Application.Commands;

/// <summary>Intention: drop one workout from a series and renumber the labels that follow.</summary>
public sealed record RemoveSeriesWorkoutCommand(Guid SeriesId, Guid WorkoutId);
