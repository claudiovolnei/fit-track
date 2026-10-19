namespace FitTrack.Application.Dtos;

/// <summary>
/// Result of assigning a series: the series itself plus whether an assignment was created now, which
/// is what decides between 201 and 200 at the edge.
/// </summary>
public sealed record AssignWorkoutSeriesResult(WorkoutSeriesDto Series, bool Created);
