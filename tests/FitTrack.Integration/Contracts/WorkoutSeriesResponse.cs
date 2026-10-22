namespace FitTrack.Integration.Contracts;

/// <summary>One prescribed exercise of a workout, with the catalog names resolved.</summary>
public sealed record PrescribedExerciseResponse(
    Guid ExerciseId,
    string ExerciseName,
    string MuscleGroupName,
    int Sets,
    int Repetitions,
    decimal? LoadKilograms,
    int RestSeconds);

/// <summary>
/// State of the workout the athlete already started today from one workout of the series.
/// </summary>
public sealed record TodayWorkoutResponse(
    Guid WorkoutSessionId,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    int ElapsedSeconds,
    bool IsRunning,
    IReadOnlyList<PerformedSetResponse> Sets)
{
    /// <summary>The exercises the athlete ticked as done in this workout.</summary>
    public IReadOnlyList<Guid> CompletedExerciseIds { get; init; } = [];

    /// <summary>How intense the athlete said the workout was. Null while the workout runs.</summary>
    public string? Intensity { get; init; }

    /// <summary>What the athlete wrote about the workout. Null when they wrote nothing.</summary>
    public string? Notes { get; init; }
}

/// <summary>One workout of a series, with its label (A..E) and the state of today.</summary>
public sealed record WorkoutSeriesWorkoutResponse(
    Guid Id,
    int Position,
    string Label,
    string Name,
    IReadOnlyList<PrescribedExerciseResponse> Exercises,
    TodayWorkoutResponse? Today);

/// <summary>Reusable workout series returned by the API.</summary>
public sealed record WorkoutSeriesResponse(
    Guid Id,
    string Name,
    Guid TrainerId,
    Guid? AthleteId,
    IReadOnlyList<WorkoutSeriesWorkoutResponse> Workouts);

/// <summary>The series an athlete is following, with the state of their 60-day term.</summary>
public sealed record MyWorkoutSeriesResponse(
    WorkoutSeriesResponse Series,
    string TrainerName,
    DateTimeOffset ExpiresAt,
    int DaysRemaining,
    bool IsExpired)
{
    /// <summary>True when the athlete is following a plan they wrote themselves.</summary>
    public bool IsSelfAuthored { get; init; }
}

/// <summary>Athlete a roster read returns, with the trainer they answer to (filled for the admin).</summary>
public sealed record AthleteResponse(
    Guid UserId,
    string Email,
    Guid? WorkoutSeriesId,
    string? WorkoutSeriesName,
    int DaysRemaining,
    string? PersonalTrainerEmail = null,
    string? EndedSeriesName = null);
