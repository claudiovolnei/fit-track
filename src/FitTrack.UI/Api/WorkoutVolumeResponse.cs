namespace FitTrack.UI.Api;

/// <summary>Wire contract: one exercise compared against the current prescription.</summary>
public sealed record ExerciseVolumeResponse(
    Guid ExerciseId,
    string ExerciseName,
    decimal PerformedKilograms,
    decimal PrescribedKilograms,
    decimal DeltaKilograms,
    decimal? DeltaPercent,
    string Status);

/// <summary>Wire contract: the session's load compared against the current prescription.</summary>
public sealed record WorkoutVolumeResponse(
    decimal PerformedKilograms,
    decimal PrescribedKilograms,
    decimal DeltaKilograms,
    decimal? DeltaPercent,
    IReadOnlyList<ExerciseVolumeResponse> Exercises);

/// <summary>Wire contract: the comparison over every session of the filtered history.</summary>
public sealed record WorkoutVolumeSummaryResponse(
    int SessionCount,
    int SetCount,
    decimal PerformedKilograms,
    decimal PrescribedKilograms,
    decimal DeltaKilograms,
    decimal? DeltaPercent,
    int SessionsWithoutPrescription);

/// <summary>Wire contract: one page of the workout history with the filtered summary.</summary>
public sealed record WorkoutHistoryResponse(
    IReadOnlyList<WorkoutSessionResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPrevious,
    bool HasNext,
    WorkoutVolumeSummaryResponse? Summary);
