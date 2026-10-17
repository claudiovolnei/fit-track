namespace FitTrack.Adapters.In.Api.Contracts;

/// <summary>HTTP contract for assigning a training plan to an athlete.</summary>
internal sealed record AssignWorkoutPlanRequest(Guid UserId);

/// <summary>HTTP contract for the muscle groups one plan option covers.</summary>
internal sealed record SetWorkoutPlanOptionRequest(IReadOnlyList<Guid> MuscleGroupIds);
