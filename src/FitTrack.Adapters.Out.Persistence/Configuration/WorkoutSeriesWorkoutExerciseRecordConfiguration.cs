using FitTrack.Adapters.Out.Persistence.Records;
using FitTrack.Domain.WorkoutPlanning;
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

        // The column default backs the migration (existing rows wake up with the default rest). The
        // sentinel is what keeps 0 — a legitimate prescription meaning no rest — from being swallowed
        // by the default: with the sentinel, 0 is an explicit value, not "unset".
        builder.Property(exercise => exercise.RestSeconds)
            .IsRequired()
            .HasDefaultValue(PrescribedExercise.DefaultRestSeconds)
            .HasSentinel(int.MinValue);

        // Restrict: an exercise a prescription points at is not deletable, the same rule the catalog
        // already follows for an exercise's muscle group.
        builder.HasOne<ExerciseRecord>()
            .WithMany()
            .HasForeignKey(exercise => exercise.ExerciseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(exercise => exercise.ExerciseId);
    }
}
