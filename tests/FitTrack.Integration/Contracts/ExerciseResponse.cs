namespace FitTrack.Integration.Contracts;

/// <summary>Catalog exercise returned by the API, with its muscle group resolved.</summary>
public sealed record ExerciseResponse(
    Guid Id,
    string Name,
    Guid MuscleGroupId,
    string MuscleGroupName,
    bool IsActive);
