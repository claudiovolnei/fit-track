namespace FitTrack.Integration.Contracts;

/// <summary>Request body for starting today's workout from one workout of the athlete's series.</summary>
public sealed record StartWorkoutRequest(Guid WorkoutSeriesWorkoutId);

/// <summary>
/// Request body for closing the workout: the whole workout travels at once — the exercises the athlete
/// marked, the repetitions and the load they really used, the level they gave the session and what
/// they wrote about it.
/// </summary>
public sealed record FinishWorkoutRequest(
    string Intensity,
    string? Notes = null,
    IReadOnlyList<PerformedExerciseRequest>? Exercises = null);

/// <summary>
/// Request body for one exercise the athlete did. The number of sets comes from the prescription —
/// the athlete adjusts the effort, not the sheet.
/// </summary>
public sealed record PerformedExerciseRequest(Guid ExerciseId, int Repetitions, decimal LoadKilograms);
