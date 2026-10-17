namespace FitTrack.Application.Commands;

/// <summary>Intention: rename an existing muscle group of the catalog.</summary>
public sealed record UpdateMuscleGroupCommand(Guid MuscleGroupId, string Name);
