using FitTrack.Adapters.Out.Persistence.Records;
using FitTrack.Domain.WorkoutCatalog;

namespace FitTrack.Adapters.Out.Persistence.Mapping;

/// <summary>Translates between the <see cref="MuscleGroup"/> aggregate and its persistence record.</summary>
internal static class MuscleGroupMapper
{
    public static MuscleGroup ToDomain(MuscleGroupRecord record) =>
        MuscleGroup.Create(new MuscleGroupId(record.Id), new MuscleGroupName(record.Name));

    public static MuscleGroupRecord ToRecord(MuscleGroup muscleGroup) => new()
    {
        Id = muscleGroup.Id.Value,
        Name = muscleGroup.Name.Value,
    };

    public static void CopyInto(MuscleGroup muscleGroup, MuscleGroupRecord record) =>
        record.Name = muscleGroup.Name.Value;
}
