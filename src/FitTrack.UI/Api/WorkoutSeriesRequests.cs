namespace FitTrack.UI.Api;

/// <summary>Wire contract: naming a series — the same body creates one and renames one.</summary>
public sealed record SetWorkoutSeriesNameRequest(string Name, Guid? AthleteId = null);

/// <summary>
/// Wire contract: creating a series. It carries more than the name because a series born for an
/// athlete can start from the sheet of the cycle they are coming from, and one the trainer prescribes
/// to several of their roster at once travels with the whole list — the single athlete, when present,
/// leads the merged list.
/// </summary>
public sealed record CreateWorkoutSeriesRequest(
    string Name,
    Guid? AthleteId = null,
    bool CopyPreviousWorkouts = false,
    IReadOnlyList<Guid>? AthleteIds = null);

/// <summary>Wire contract: one prescribed exercise of a workout.</summary>
public sealed record PrescribedExerciseRequest(Guid ExerciseId, int Sets, int Repetitions, decimal? LoadKilograms, int? RestSeconds = null);

/// <summary>Wire contract: the name and the prescription of one workout of a series.</summary>
public sealed record SetSeriesWorkoutRequest(string Name, IReadOnlyList<PrescribedExerciseRequest> Exercises);
/// <summary>Wire contract: starting today's workout from one workout of the athlete's series.</summary>
public sealed record StartWorkoutRequest(Guid WorkoutSeriesWorkoutId);

/// <summary>
/// Wire contract: closing the workout. The whole workout travels at once — the exercises the athlete
/// marked, the repetitions and the load they really used, the level they gave the session and what
/// they wrote about it.
/// </summary>
public sealed record FinishWorkoutRequest(
    string Intensity,
    string? Notes = null,
    IReadOnlyList<PerformedExerciseRequest>? Exercises = null);

/// <summary>
/// Wire contract: one exercise the athlete did in the workout. The number of sets comes from the
/// prescription — the athlete adjusts the effort, not the sheet.
/// </summary>
public sealed record PerformedExerciseRequest(Guid ExerciseId, int Repetitions, decimal LoadKilograms);
