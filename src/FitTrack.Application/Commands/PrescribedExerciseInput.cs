namespace FitTrack.Application.Commands;

/// <summary>
/// One prescribed exercise of a workout, as the edge hands it in. It is the input shape only: the
/// domain turns it into a <c>PrescribedExercise</c> and states the ranges there.
/// </summary>
public sealed record PrescribedExerciseInput(
    Guid ExerciseId,
    int Sets,
    int Repetitions,
    decimal? LoadKilograms);
