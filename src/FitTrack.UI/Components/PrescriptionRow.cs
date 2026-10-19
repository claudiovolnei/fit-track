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
}
