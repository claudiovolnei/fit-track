namespace FitTrack.UI.Api;

/// <summary>Wire contract: a catalog exercise with its muscle group resolved.</summary>
public sealed record ExerciseResponse(
    Guid Id,
    string Name,
    Guid MuscleGroupId,
    string MuscleGroupName,
    bool IsActive);
