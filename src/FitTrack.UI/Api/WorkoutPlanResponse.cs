namespace FitTrack.UI.Api;

/// <summary>Wire contract: the workout already started today from a plan option.</summary>
public sealed record TodayWorkoutResponse(
    Guid WorkoutSessionId,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    int ElapsedSeconds,
    bool IsRunning,
    IReadOnlyList<PerformedSetResponse> Sets);

/// <summary>Wire contract: one option of a training plan, with the groups it covers and today's state.</summary>
public sealed record WorkoutPlanOptionResponse(
    Guid Id,
    int Position,
    IReadOnlyList<MuscleGroupResponse> MuscleGroups,
    TodayWorkoutResponse? Today);

/// <summary>Wire contract: a training plan with its options in order.</summary>
public sealed record WorkoutPlanResponse(
    Guid Id,
    Guid UserId,
    Guid TrainerId,
    IReadOnlyList<WorkoutPlanOptionResponse> Options);

/// <summary>Wire contract: an account a plan can be assigned to.</summary>
public sealed record AthleteResponse(Guid UserId, string Email, bool HasPlan);
