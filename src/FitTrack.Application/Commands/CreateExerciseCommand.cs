namespace FitTrack.Application.Commands;

/// <summary>Intention: register a new exercise in the catalog, linked to a muscle group.</summary>
public sealed record CreateExerciseCommand(string Name, Guid MuscleGroupId);
