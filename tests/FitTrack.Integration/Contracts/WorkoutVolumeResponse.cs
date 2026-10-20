namespace FitTrack.Integration.Contracts;

/// <summary>One exercise compared against the current prescription.</summary>
public sealed record ExerciseVolumeResponse(
    Guid ExerciseId,
    string ExerciseName,
    decimal PerformedKilograms,
    decimal PrescribedKilograms,
    decimal DeltaKilograms,
    decimal? DeltaPercent,
    string Status);

/// <summary>The session's load compared against the current prescription.</summary>
public sealed record WorkoutVolumeResponse(
    decimal PerformedKilograms,
    decimal PrescribedKilograms,
    decimal DeltaKilograms,
    decimal? DeltaPercent,
    IReadOnlyList<ExerciseVolumeResponse> Exercises);

/// <summary>The comparison over every session of the filtered history.</summary>
public sealed record WorkoutVolumeSummaryResponse(
    int SessionCount,
    int SetCount,
    decimal PerformedKilograms,
    decimal PrescribedKilograms,
    decimal DeltaKilograms,
    decimal? DeltaPercent,
    int SessionsWithoutPrescription);

/// <summary>One page of a collection, as the paginated envelope returns it.</summary>
public sealed record PagedResultResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPrevious,
    bool HasNext);

/// <summary>One page of the workout history, with the summary of every filtered session.</summary>
public sealed record WorkoutHistoryResponse(
    IReadOnlyList<WorkoutSessionResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPrevious,
    bool HasNext,
    WorkoutVolumeSummaryResponse? Summary);
