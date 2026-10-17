namespace FitTrack.UI.Api;

/// <summary>Wire contract: editing a catalog exercise.</summary>
public sealed record UpdateExerciseRequest(string Name, Guid MuscleGroupId, bool IsActive);
