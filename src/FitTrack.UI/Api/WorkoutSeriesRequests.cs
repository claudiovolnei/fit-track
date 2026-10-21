namespace FitTrack.UI.Api;

/// <summary>Wire contract: naming a series — the same body creates one and renames one.</summary>
public sealed record SetWorkoutSeriesNameRequest(string Name, Guid? AthleteId = null);

/// <summary>Wire contract: one prescribed exercise of a workout.</summary>
public sealed record PrescribedExerciseRequest(Guid ExerciseId, int Sets, int Repetitions, decimal? LoadKilograms, int? RestSeconds = null);

/// <summary>Wire contract: the name and the prescription of one workout of a series.</summary>
public sealed record SetSeriesWorkoutRequest(string Name, IReadOnlyList<PrescribedExerciseRequest> Exercises);
/// <summary>Wire contract: starting today's workout from one workout of the athlete's series.</summary>
public sealed record StartWorkoutRequest(Guid WorkoutSeriesWorkoutId);

/// <summary>Wire contract: one set performed in the workout in progress. The repetitions come from the prescription.</summary>
public sealed record LogPerformedSetRequest(Guid ExerciseId, decimal LoadKilograms);
