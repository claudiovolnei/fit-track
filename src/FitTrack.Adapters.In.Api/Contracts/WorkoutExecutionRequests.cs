namespace FitTrack.Adapters.In.Api.Contracts;

/// <summary>HTTP contract for starting today's workout from one workout of the athlete's series.</summary>
internal sealed record StartWorkoutRequest(Guid WorkoutSeriesWorkoutId);

/// <summary>
/// HTTP contract for a set the athlete performed in the workout in progress. The load is always the
/// athlete's; the repetitions are optional — when they are left out, the ones the trainer prescribed
/// are used.
/// </summary>
internal sealed record LogPerformedSetRequest(Guid ExerciseId, decimal LoadKilograms, int? Repetitions = null);

/// <summary>HTTP contract for ticking an exercise of the workout in progress as done.</summary>
internal sealed record SetExerciseCompletionRequest(bool Completed);
