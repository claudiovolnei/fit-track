namespace FitTrack.Application.Commands;

/// <summary>Intention: change the editable fields of an existing catalog exercise.</summary>
public sealed record UpdateExerciseCommand(Guid ExerciseId, string Name, Guid MuscleGroupId, bool IsActive);
