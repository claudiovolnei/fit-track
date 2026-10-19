using FitTrack.Domain.Common;
using FitTrack.Domain.WorkoutCatalog;

namespace FitTrack.Domain.WorkoutPlanning;

/// <summary>
/// Value object for what the trainer prescribes for one exercise: how many sets, how many
/// repetitions and the target load. The load is nullable because a bodyweight exercise has no
/// weight to prescribe. Prescribing is not performing — the execution context keeps its own,
/// mandatory <c>Load</c> on <c>PerformedSet</c>, and the two must not share a value object.
/// </summary>
public readonly record struct PrescribedExercise
{
    public const int MinSets = 1;

    public const int MaxSets = 20;

    public const int MinRepetitions = 1;

    public const int MaxRepetitions = 100;

    public PrescribedExercise(ExerciseId exerciseId, int sets, int repetitions, decimal? loadKilograms)
    {
        if (exerciseId.Value == Guid.Empty)
        {
            throw new DomainException(
                "workout_series.exercise.required",
                "A prescribed exercise must reference a catalog exercise.");
        }

        if (sets < MinSets || sets > MaxSets)
        {
            throw new DomainException(
                "workout_series.exercise.prescription.invalid",
                $"Prescribed sets must be between {MinSets} and {MaxSets}.");
        }

        if (repetitions < MinRepetitions || repetitions > MaxRepetitions)
        {
            throw new DomainException(
                "workout_series.exercise.prescription.invalid",
                $"Prescribed repetitions must be between {MinRepetitions} and {MaxRepetitions}.");
        }

        if (loadKilograms is < 0)
        {
            throw new DomainException(
                "workout_series.exercise.prescription.invalid",
                "Prescribed load cannot be negative.");
        }

        ExerciseId = exerciseId;
        Sets = sets;
        Repetitions = repetitions;
        LoadKilograms = loadKilograms;
    }

    public ExerciseId ExerciseId { get; }

    public int Sets { get; }

    public int Repetitions { get; }

    /// <summary>Target load in kilograms, or null for a bodyweight exercise.</summary>
    public decimal? LoadKilograms { get; }
}
