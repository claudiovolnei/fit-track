namespace FitTrack.UI.Components;

/// <summary>
/// One line of a prescription being typed into a form: what the trainer prescribes for one exercise.
/// The load is nullable because a bodyweight exercise has no weight to prescribe.
/// </summary>
public sealed class PrescriptionRow
{
    public Guid ExerciseId { get; set; }

    public int Sets { get; set; } = 3;

    public int Repetitions { get; set; } = 10;

    public decimal? LoadKilograms { get; set; } = 20m;

    /// <summary>
    /// The rest between sets, in seconds; 0 means the next set starts right away. The range and the
    /// default belong to the domain (<c>PrescribedExercise.MinRestSeconds</c>,
    /// <c>MaxRestSeconds</c> and <c>DefaultRestSeconds</c>) — the API refuses whatever is out of it.
    /// </summary>
    public int RestSeconds { get; set; } = 60;
}
