using FitTrack.Adapters.Out.Persistence.Records;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitTrack.Adapters.Out.Persistence.Configuration;

internal sealed class WorkoutSeriesWorkoutExerciseRecordConfiguration
    : IEntityTypeConfiguration<WorkoutSeriesWorkoutExerciseRecord>
{
    public void Configure(EntityTypeBuilder<WorkoutSeriesWorkoutExerciseRecord> builder)
    {
        builder.ToTable("workout_series_workout_exercises");

        builder.HasKey(exercise => new { exercise.WorkoutSeriesWorkoutId, exercise.ExerciseId });

        builder.Property(exercise => exercise.Sets)
            .IsRequired();

        builder.Property(exercise => exercise.Repetitions)
            .IsRequired();

        builder.Property(exercise => exercise.LoadKilograms)
            .HasPrecision(7, 2);

        // Restrict: an exercise a prescription points at is not deletable, the same rule the catalog
        // already follows for an exercise's muscle group.
        builder.HasOne<ExerciseRecord>()
            .WithMany()
            .HasForeignKey(exercise => exercise.ExerciseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(exercise => exercise.ExerciseId);
    }
}
