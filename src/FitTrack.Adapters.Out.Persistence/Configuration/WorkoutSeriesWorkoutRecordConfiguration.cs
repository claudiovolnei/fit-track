using FitTrack.Adapters.Out.Persistence.Records;
using FitTrack.Domain.WorkoutPlanning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitTrack.Adapters.Out.Persistence.Configuration;

internal sealed class WorkoutSeriesWorkoutRecordConfiguration
    : IEntityTypeConfiguration<WorkoutSeriesWorkoutRecord>
{
    public void Configure(EntityTypeBuilder<WorkoutSeriesWorkoutRecord> builder)
    {
        builder.ToTable("workout_series_workouts");

        builder.HasKey(workout => workout.Id);

        builder.Property(workout => workout.Name)
            .HasMaxLength(WorkoutName.MaxLength)
            .IsRequired();

        // Not unique on purpose: removing a workout renumbers the rest, and a unique index would make
        // the renumbering order matter. The aggregate is the one that keeps positions contiguous.
        builder.HasIndex(workout => new { workout.WorkoutSeriesId, workout.Position });

        builder.HasMany(workout => workout.Exercises)
            .WithOne()
            .HasForeignKey(exercise => exercise.WorkoutSeriesWorkoutId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
