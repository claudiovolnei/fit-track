namespace FitTrack.Adapters.In.Api.Contracts;

/// <summary>
/// HTTP contract for naming a series. Creating and renaming share the body, because a series is born
/// with nothing but a name — the workouts come later, one request each.
/// </summary>
internal sealed record SetWorkoutSeriesNameRequest(string Name, Guid? AthleteId = null);

/// <summary>HTTP contract for the name and the prescription of one workout of a series.</summary>
internal sealed record SetSeriesWorkoutRequest(string Name, IReadOnlyList<PrescribedExerciseRequest> Exercises);

/// <summary>
/// HTTP contract for one prescribed exercise. The load is nullable because a bodyweight exercise has no
/// weight to prescribe; the accepted ranges stay in the domain, which is where the rule lives. The rest
/// is optional on the wire: omitted means the domain default.
/// </summary>
internal sealed record PrescribedExerciseRequest(Guid ExerciseId, int Sets, int Repetitions, decimal? LoadKilograms, int? RestSeconds = null);
