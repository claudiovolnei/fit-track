namespace FitTrack.Application.Dtos;

/// <summary>
/// Result of starting today's workout: the session plus whether it was started now or was already
/// running, which is what decides between 201 and 200 at the edge.
/// </summary>
public sealed record StartWorkoutResult(WorkoutSessionDto Session, bool Started);
