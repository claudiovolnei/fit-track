namespace FitTrack.Application.Commands;

/// <summary>Intention: remove a muscle group that no exercise of the catalog references.</summary>
public sealed record DeleteMuscleGroupCommand(Guid MuscleGroupId);
