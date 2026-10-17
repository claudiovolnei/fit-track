namespace FitTrack.UI.Api;

/// <summary>Wire contract: creating a catalog exercise linked to a muscle group.</summary>
public sealed record CreateExerciseRequest(string Name, Guid MuscleGroupId);
