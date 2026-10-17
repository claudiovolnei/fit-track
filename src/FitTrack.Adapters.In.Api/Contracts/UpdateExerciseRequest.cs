namespace FitTrack.Adapters.In.Api.Contracts;

/// <summary>HTTP contract for editing a catalog exercise.</summary>
internal sealed record UpdateExerciseRequest(string Name, Guid MuscleGroupId, bool IsActive);
