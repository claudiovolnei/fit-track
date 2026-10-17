namespace FitTrack.Application.Dtos;

/// <summary>
/// Result of assigning a plan: the plan itself plus whether it was created now or already existed,
/// which is what decides between 201 and 200 at the edge.
/// </summary>
public sealed record AssignWorkoutPlanResult(WorkoutPlanDto Plan, bool Created);
