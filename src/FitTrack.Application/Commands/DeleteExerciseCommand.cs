namespace FitTrack.Application.Commands;

/// <summary>Intention: remove an exercise that no prescription and no performed set references.</summary>
public sealed record DeleteExerciseCommand(Guid ExerciseId);
