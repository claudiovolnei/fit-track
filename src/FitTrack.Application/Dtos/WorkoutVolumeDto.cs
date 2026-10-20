namespace FitTrack.Application.Dtos;

/// <summary>
/// One exercise of a session, with what the trainer prescribed and what the athlete actually moved.
/// <see cref="Status"/> is a stable code the UI translates: matched, bodyweight, notPerformed or
/// notPrescribed.
/// </summary>
public sealed record ExerciseVolumeDto(
    Guid ExerciseId,
    string ExerciseName,
    decimal PerformedKilograms,
    decimal PrescribedKilograms,
    decimal DeltaKilograms,
    decimal? DeltaPercent,
    string Status);

/// <summary>
/// The session's load compared against the current prescription of its workout. Null when the
/// session has no workout reference (legacy data or a workout removed from the series).
/// </summary>
public sealed record WorkoutVolumeDto(
    decimal PerformedKilograms,
    decimal PrescribedKilograms,
    decimal DeltaKilograms,
    decimal? DeltaPercent,
    IReadOnlyList<ExerciseVolumeDto> Exercises);

/// <summary>The comparison over every session in the filtered history, not only the current page.</summary>
public sealed record WorkoutVolumeSummaryDto(
    int SessionCount,
    int SetCount,
    decimal PerformedKilograms,
    decimal PrescribedKilograms,
    decimal DeltaKilograms,
    decimal? DeltaPercent,
    int SessionsWithoutPrescription);
