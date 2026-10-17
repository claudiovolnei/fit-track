namespace FitTrack.Application.Dtos;

/// <summary>Read model of a single performed set within a workout session.</summary>
public sealed record PerformedSetDto(Guid ExerciseId, decimal LoadKilograms, int Repetitions);
