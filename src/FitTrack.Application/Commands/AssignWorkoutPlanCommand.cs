namespace FitTrack.Application.Commands;

/// <summary>Intention: assign a training plan to an athlete, creating it if there is none yet.</summary>
public sealed record AssignWorkoutPlanCommand(Guid UserId);
