namespace FitTrack.Integration.Contracts;

/// <summary>Request body for starting today's workout from one option of the athlete's plan.</summary>
public sealed record StartWorkoutRequest(Guid WorkoutPlanOptionId);

/// <summary>Request body for a set the athlete performed in the workout in progress.</summary>
public sealed record LogPerformedSetRequest(Guid ExerciseId, decimal LoadKilograms, int Repetitions);
