namespace FitTrack.Application.Commands;

/// <summary>Intention: start today's workout from one workout of the series the athlete received.</summary>
public sealed record StartWorkoutCommand(Guid WorkoutSeriesWorkoutId);
