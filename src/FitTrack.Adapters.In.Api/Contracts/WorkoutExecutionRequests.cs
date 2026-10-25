namespace FitTrack.Adapters.In.Api.Contracts;

/// <summary>HTTP contract for starting today's workout from one workout of the athlete's series.</summary>
internal sealed record StartWorkoutRequest(Guid WorkoutSeriesWorkoutId);

/// <summary>
/// HTTP contract for closing the workout. The whole workout arrives at once: the exercises the
/// athlete marked, the repetitions and the load they really used, the level they gave the session
/// and what they wrote about it.
/// </summary>
internal sealed record FinishWorkoutRequest(
    string? Intensity,
    string? Notes,
    IReadOnlyList<PerformedExerciseRequest>? Exercises);

/// <summary>
/// HTTP contract for one exercise the athlete did in the workout. The number of sets comes from the
/// prescription: the athlete adjusts the effort, not the sheet.
/// </summary>
internal sealed record PerformedSetRequest(int Repetitions, decimal LoadKilograms);

internal sealed record PerformedExerciseRequest(Guid ExerciseId, IReadOnlyList<PerformedSetRequest> Sets);
