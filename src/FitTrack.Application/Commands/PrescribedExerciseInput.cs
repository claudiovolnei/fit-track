namespace FitTrack.Application.Commands;

/// <summary>
/// One prescribed exercise of a workout, as the edge hands it in. It is the input shape only: the
/// domain turns it into a <c>PrescribedExercise</c> and states the ranges there. The rest is optional
/// on the wire: omitted means the default the domain owns — it is not normalized here.
/// </summary>
public sealed record PrescribedExerciseInput(
    Guid ExerciseId,
    int Sets,
    int Repetitions,
    decimal? LoadKilograms,
    int? RestSeconds = null);
