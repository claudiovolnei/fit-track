using FitTrack.Application.Dtos;
using FitTrack.Domain.WorkoutCatalog;

namespace FitTrack.Application.Mapping;

/// <summary>
/// Translates the <see cref="Exercise"/> aggregate into its application read model. The aggregate
/// only holds the muscle group id, so the use case resolves the name and passes it in.
/// </summary>
internal static class ExerciseMapping
{
    public static ExerciseDto ToDto(this Exercise exercise, MuscleGroupName muscleGroupName) =>
        new(
            exercise.Id.Value,
            exercise.Name.Value,
            exercise.MuscleGroupId.Value,
            muscleGroupName.Value,
            exercise.IsActive);
}
