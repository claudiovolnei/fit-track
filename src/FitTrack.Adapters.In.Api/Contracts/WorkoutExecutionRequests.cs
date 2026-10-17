namespace FitTrack.Adapters.In.Api.Contracts;

/// <summary>HTTP contract for starting today's workout from one option of the athlete's plan.</summary>
internal sealed record StartWorkoutRequest(Guid WorkoutPlanOptionId);

/// <summary>HTTP contract for a set the athlete performed in the workout in progress.</summary>
internal sealed record LogPerformedSetRequest(Guid ExerciseId, decimal LoadKilograms, int Repetitions);
