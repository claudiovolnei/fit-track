namespace FitTrack.Integration.Contracts;

/// <summary>
/// Request body for naming a series. Creating and renaming share it, because a series is born with
/// nothing but a name.
/// </summary>
public sealed record SetWorkoutSeriesNameRequest(string Name, Guid? AthleteId = null);

/// <summary>
/// Request body for creating a series. It carries more than the name because a series born for an
/// athlete can start from the sheet of the cycle they are coming from, and one the trainer prescribes
/// to several of their roster at once travels with the whole list — the single athlete, when present,
/// leads the merged list.
/// </summary>
public sealed record CreateWorkoutSeriesRequest(
    string Name,
    Guid? AthleteId = null,
    bool CopyPreviousWorkouts = false,
    IReadOnlyList<Guid>? AthleteIds = null);

/// <summary>
/// Request body for one prescribed exercise. The load is nullable because a bodyweight exercise has no
/// weight to prescribe.
/// </summary>
public sealed record PrescribedExerciseRequest(Guid ExerciseId, int Sets, int Repetitions, decimal? LoadKilograms, int? RestSeconds = null);

/// <summary>Request body for the name and the prescription of one workout of a series.</summary>
public sealed record SetSeriesWorkoutRequest(string Name, IReadOnlyList<PrescribedExerciseRequest> Exercises);
