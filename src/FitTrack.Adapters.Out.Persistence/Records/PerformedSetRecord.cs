namespace FitTrack.Adapters.Out.Persistence.Records;

/// <summary>
/// Persistence model for one performed set, owned by a <see cref="WorkoutSessionRecord"/>.
/// The key is (session, position) because a set is a value inside the aggregate: its order in
/// the workout is part of the data, and a random surrogate key would scramble it on read.
/// </summary>
public sealed class PerformedSetRecord
{
    public Guid WorkoutSessionId { get; set; }

    public int Position { get; set; }

    public Guid ExerciseId { get; set; }

    public decimal LoadKilograms { get; set; }

    public int Repetitions { get; set; }
}
