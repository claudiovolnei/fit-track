namespace FitTrack.Integration.Contracts;

/// <summary>Request body for creating a catalog exercise linked to a muscle group.</summary>
public sealed record CreateExerciseRequest(string Name, Guid MuscleGroupId);
