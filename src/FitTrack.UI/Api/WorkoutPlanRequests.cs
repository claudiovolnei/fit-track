namespace FitTrack.UI.Api;

/// <summary>Wire contract: assigning a training plan to an athlete.</summary>
public sealed record AssignWorkoutPlanRequest(Guid UserId);

/// <summary>Wire contract: the muscle groups one plan option covers.</summary>
public sealed record SetWorkoutPlanOptionRequest(IReadOnlyList<Guid> MuscleGroupIds);

/// <summary>Wire contract: starting today's workout from one option of the plan.</summary>
public sealed record StartWorkoutRequest(Guid WorkoutPlanOptionId);

/// <summary>Wire contract: one set performed in the workout in progress.</summary>
public sealed record LogPerformedSetRequest(Guid ExerciseId, decimal LoadKilograms, int Repetitions);
