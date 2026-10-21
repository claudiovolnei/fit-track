namespace FitTrack.Application.Dtos;

/// <summary>
/// Read model of one prescribed exercise, with the catalog names resolved so the screen does not have
/// to hold the catalog. <see cref="LoadKilograms"/> is null for a bodyweight exercise, and
/// <see cref="RestSeconds"/> is 0 when there is no rest to count.
/// </summary>
public sealed record PrescribedExerciseDto(
    Guid ExerciseId,
    string ExerciseName,
    string MuscleGroupName,
    int Sets,
    int Repetitions,
    decimal? LoadKilograms,
    int RestSeconds);
