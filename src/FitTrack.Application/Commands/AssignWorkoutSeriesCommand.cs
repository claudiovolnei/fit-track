namespace FitTrack.Application.Commands;

/// <summary>Intention: put a series in the hands of one athlete, starting their 60-day term.</summary>
public sealed record AssignWorkoutSeriesCommand(Guid SeriesId, Guid UserId);
