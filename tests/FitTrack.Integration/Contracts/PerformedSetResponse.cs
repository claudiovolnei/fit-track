namespace FitTrack.Integration.Contracts;

/// <summary>Performed set returned inside a workout session.</summary>
public sealed record PerformedSetResponse(Guid ExerciseId, decimal LoadKilograms, int Repetitions);
