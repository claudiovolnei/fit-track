namespace FitTrack.Application.Commands;

/// <summary>Intention: start today's workout from one option of the plan the athlete received.</summary>
public sealed record StartWorkoutCommand(Guid WorkoutPlanOptionId);
