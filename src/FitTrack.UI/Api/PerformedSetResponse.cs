namespace FitTrack.UI.Api;

/// <summary>Wire contract: a performed set inside a workout session.</summary>
public sealed record PerformedSetResponse(Guid ExerciseId, decimal LoadKilograms, int Repetitions);
