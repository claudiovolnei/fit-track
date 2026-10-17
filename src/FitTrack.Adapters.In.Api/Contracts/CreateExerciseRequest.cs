namespace FitTrack.Adapters.In.Api.Contracts;

/// <summary>HTTP contract for adding a catalog exercise linked to a muscle group.</summary>
internal sealed record CreateExerciseRequest(string Name, Guid MuscleGroupId);
