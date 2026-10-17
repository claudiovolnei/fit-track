namespace FitTrack.Integration.Contracts;

/// <summary>Request body for assigning a training plan to an athlete.</summary>
public sealed record AssignWorkoutPlanRequest(Guid UserId);

/// <summary>Request body with the muscle groups one plan option covers.</summary>
public sealed record SetWorkoutPlanOptionRequest(IReadOnlyList<Guid> MuscleGroupIds);
