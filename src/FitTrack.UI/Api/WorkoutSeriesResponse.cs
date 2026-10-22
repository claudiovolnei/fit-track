namespace FitTrack.UI.Api;

/// <summary>Wire contract: one prescribed exercise, with the catalog names already resolved.</summary>
public sealed record PrescribedExerciseResponse(
    Guid ExerciseId,
    string ExerciseName,
    string MuscleGroupName,
    int Sets,
    int Repetitions,
    decimal? LoadKilograms,
    int RestSeconds);

/// <summary>Wire contract: the workout already started today from one workout of the series.</summary>
public sealed record TodayWorkoutResponse(
    Guid WorkoutSessionId,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    int ElapsedSeconds,
    bool IsRunning,
    IReadOnlyList<PerformedSetResponse> Sets);

/// <summary>Wire contract: one specific workout of a series, labelled A..E by its position.</summary>
public sealed record WorkoutSeriesWorkoutResponse(
    Guid Id,
    int Position,
    string Label,
    string Name,
    IReadOnlyList<PrescribedExerciseResponse> Exercises,
    TodayWorkoutResponse? Today);

/// <summary>Wire contract: a reusable series authored by a personal trainer.</summary>
public sealed record WorkoutSeriesResponse(
    Guid Id,
    string Name,
    Guid TrainerId,
    Guid? AthleteId,
    IReadOnlyList<WorkoutSeriesWorkoutResponse> Workouts);

/// <summary>Wire contract: the series an athlete follows, with the state of its 60-day term.</summary>
public sealed record MyWorkoutSeriesResponse(
    WorkoutSeriesResponse Series,
    string TrainerName,
    DateTimeOffset ExpiresAt,
    int DaysRemaining,
    bool IsExpired);

/// <summary>Wire contract: an athlete the roster grants access to. The trainer's e-mail is filled only for the admin read.</summary>
public sealed record AthleteResponse(
    Guid UserId,
    string Email,
    Guid? WorkoutSeriesId,
    string? WorkoutSeriesName,
    int DaysRemaining,
    string? PersonalTrainerEmail = null,
    string? EndedSeriesName = null);
