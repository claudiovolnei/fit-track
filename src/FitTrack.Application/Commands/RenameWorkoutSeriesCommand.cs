namespace FitTrack.Application.Commands;

/// <summary>Intention: rename a series the personal trainer authored.</summary>
public sealed record RenameWorkoutSeriesCommand(Guid SeriesId, string Name);
