namespace FitTrack.Integration.Contracts;

/// <summary>Request body for starting today's workout from one workout of the athlete's series.</summary>
public sealed record StartWorkoutRequest(Guid WorkoutSeriesWorkoutId);

/// <summary>
/// Request body for a set the athlete performed. The repetitions are optional: when they are left out,
/// the ones the trainer prescribed are used; when the athlete fills them in, the set is recorded with
/// what was really done.
/// </summary>
public sealed record LogPerformedSetRequest(Guid ExerciseId, decimal LoadKilograms, int? Repetitions = null);

/// <summary>Request body for ticking an exercise of the workout in progress as done.</summary>
public sealed record SetExerciseCompletionRequest(bool Completed);
