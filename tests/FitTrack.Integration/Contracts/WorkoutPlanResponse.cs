namespace FitTrack.Integration.Contracts;

/// <summary>State of the workout already started today from a plan option.</summary>
public sealed record TodayWorkoutResponse(
    Guid WorkoutSessionId,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    int ElapsedSeconds,
    bool IsRunning,
    IReadOnlyList<PerformedSetResponse> Sets);

/// <summary>One option of a plan, with the muscle groups and the state of today.</summary>
public sealed record WorkoutPlanOptionResponse(
    Guid Id,
    int Position,
    IReadOnlyList<MuscleGroupResponse> MuscleGroups,
    TodayWorkoutResponse? Today);

/// <summary>Training plan returned by the API.</summary>
public sealed record WorkoutPlanResponse(
    Guid Id,
    Guid UserId,
    Guid TrainerId,
    IReadOnlyList<WorkoutPlanOptionResponse> Options);

/// <summary>Athlete available to receive a plan.</summary>
public sealed record AthleteResponse(Guid UserId, string Email, bool HasPlan);
