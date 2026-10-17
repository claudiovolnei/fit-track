using FitTrack.Application.Dtos;
using FitTrack.Domain.WorkoutCatalog;

namespace FitTrack.Application.Mapping;

/// <summary>Translates the <see cref="MuscleGroup"/> aggregate into its application read model.</summary>
internal static class MuscleGroupMapping
{
    public static MuscleGroupDto ToDto(this MuscleGroup muscleGroup) =>
        new(muscleGroup.Id.Value, muscleGroup.Name.Value);
}
