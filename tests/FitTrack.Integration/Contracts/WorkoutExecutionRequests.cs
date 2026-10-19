namespace FitTrack.Integration.Contracts;

/// <summary>Request body for starting today's workout from one workout of the athlete's series.</summary>
public sealed record StartWorkoutRequest(Guid WorkoutSeriesWorkoutId);

/// <summary>
/// Request body for a set the athlete performed. The repetitions are not sent: they come from the
/// prescription the trainer wrote, so the load is the only thing the athlete fills in.
/// </summary>
public sealed record LogPerformedSetRequest(Guid ExerciseId, decimal LoadKilograms);
