namespace FitTrack.Adapters.Out.Persistence.Records;

/// <summary>
/// Persistence model for one exercise the athlete ticked as done inside a session. The key is
/// (session, exercise) because the tick is a value inside the aggregate: the same exercise is ticked
/// once, and the key leaves no room for a second row of the same exercise.
/// </summary>
public sealed class CompletedExerciseRecord
{
    public Guid WorkoutSessionId { get; set; }

    public Guid ExerciseId { get; set; }
}
