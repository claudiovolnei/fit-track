namespace FitTrack.Application.Dtos;

/// <summary>Read model of a catalog exercise for the edge, with its muscle group resolved.</summary>
public sealed record ExerciseDto(
    Guid Id,
    string Name,
    Guid MuscleGroupId,
    string MuscleGroupName,
    bool IsActive);
