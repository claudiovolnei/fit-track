using FitTrack.Integration.Contracts;

namespace FitTrack.Integration;

/// <summary>
/// A prescribed exercise with the ranges a trainer would actually type, so a test says which exercise it
/// prescribes instead of spelling three numbers the assertion does not care about.
/// </summary>
internal static class Prescriptions
{
    public static PrescribedExerciseRequest For(
        Guid exerciseId,
        int sets = 3,
        int repetitions = 12,
        decimal? loadKilograms = 40m) =>
        new(exerciseId, sets, repetitions, loadKilograms);
}
