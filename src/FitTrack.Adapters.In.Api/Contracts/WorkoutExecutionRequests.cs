namespace FitTrack.Adapters.In.Api.Contracts;

/// <summary>HTTP contract for starting today's workout from one workout of the athlete's series.</summary>
internal sealed record StartWorkoutRequest(Guid WorkoutSeriesWorkoutId);

/// <summary>
/// HTTP contract for a set the athlete performed in the workout in progress. The athlete only fills in
/// the load; the repetitions come from the prescribed workout.
/// </summary>
internal sealed record LogPerformedSetRequest(Guid ExerciseId, decimal LoadKilograms);
