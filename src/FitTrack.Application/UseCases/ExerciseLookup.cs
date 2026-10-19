using FitTrack.Application.Exceptions;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Application.UseCases;

/// <summary>
/// Checks that the exercises a prescription references exist. A workout prescribes exercises, and an
/// unknown reference is refused here so the caller gets a stated rule instead of a raw foreign-key
/// error from the database.
/// </summary>
internal static class ExerciseLookup
{
    public static async Task RequireAllAsync(
        IExerciseRepository exercises,
        IReadOnlyList<PrescribedExercise> prescribed,
        CancellationToken cancellationToken)
    {
        if (prescribed.Count == 0)
        {
            return;
        }

        var ids = prescribed.Select(exercise => exercise.ExerciseId).Distinct().ToArray();
        var found = await exercises.GetByIdsAsync(ids, cancellationToken);
        var known = found.Select(exercise => exercise.Id).ToHashSet();

        var missing = ids.Where(id => !known.Contains(id)).Select(id => id.Value).ToArray();

        if (missing.Length > 0)
        {
            throw new NotFoundException($"Exercise(s) '{string.Join("', '", missing)}' were not found.");
        }
    }
}
