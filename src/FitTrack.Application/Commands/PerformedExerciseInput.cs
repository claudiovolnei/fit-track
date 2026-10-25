using FitTrack.Domain.WorkoutCatalog;
using FitTrack.Domain.WorkoutExecution;

namespace FitTrack.Application.Commands;

/// <summary>
/// What the athlete did of one prescribed exercise: the effort of each performed set. The
/// number of sets comes from the prescription — the athlete adjusts the effort of each
/// one, not how many there are.
/// </summary>
public sealed record PerformedExerciseInput(
    Guid ExerciseId,
    IReadOnlyList<PerformedSetInput> Sets)
{
    /// <summary>The exercise the entry belongs to, in the domain's own vocabulary.</summary>
    public ExerciseId ToExerciseId() => new(ExerciseId);

    /// <summary>Turns the input into the domain value, validating each set's load.</summary>
    public FitTrack.Domain.WorkoutExecution.PerformedExercise ToPerformedExercise()
    {
        var sets = Sets.Select(set => (new Load(set.LoadKilograms), new Repetitions(set.Repetitions))).ToList();
        return new FitTrack.Domain.WorkoutExecution.PerformedExercise(ToExerciseId(), sets);
    }
}

/// <summary>The effort of one performed set: repetitions and load in kilograms.</summary>
public sealed record PerformedSetInput(int Repetitions, decimal LoadKilograms);