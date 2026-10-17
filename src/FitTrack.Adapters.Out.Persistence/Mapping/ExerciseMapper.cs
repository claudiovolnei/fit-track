using FitTrack.Adapters.Out.Persistence.Records;
using FitTrack.Domain.WorkoutCatalog;

namespace FitTrack.Adapters.Out.Persistence.Mapping;

/// <summary>Translates between the <see cref="Exercise"/> aggregate and its persistence record.</summary>
internal static class ExerciseMapper
{
    public static Exercise ToDomain(ExerciseRecord record)
    {
        var exercise = Exercise.Create(
            new ExerciseId(record.Id),
            new ExerciseName(record.Name),
            new MuscleGroupId(record.MuscleGroupId));

        if (!record.IsActive)
        {
            exercise.Deactivate();
        }

        return exercise;
    }

    public static ExerciseRecord ToRecord(Exercise exercise) => new()
    {
        Id = exercise.Id.Value,
        Name = exercise.Name.Value,
        MuscleGroupId = exercise.MuscleGroupId.Value,
        IsActive = exercise.IsActive,
    };

    public static void CopyInto(Exercise exercise, ExerciseRecord record)
    {
        record.Name = exercise.Name.Value;
        record.MuscleGroupId = exercise.MuscleGroupId.Value;
        record.IsActive = exercise.IsActive;
    }
}
