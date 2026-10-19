using FitTrack.Application.Commands;
using FitTrack.Domain.WorkoutCatalog;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Application.Mapping;

/// <summary>
/// Turns the edge input of a prescription into the domain value object, where the ranges are stated.
/// </summary>
internal static class PrescribedExerciseMapping
{
    public static PrescribedExercise[] ToPrescribed(this IReadOnlyList<PrescribedExerciseInput> inputs) =>
        [.. inputs.Select(input => new PrescribedExercise(
            new ExerciseId(input.ExerciseId),
            input.Sets,
            input.Repetitions,
            input.LoadKilograms))];
}
