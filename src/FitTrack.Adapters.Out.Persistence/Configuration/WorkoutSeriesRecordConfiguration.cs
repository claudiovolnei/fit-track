using FitTrack.Adapters.Out.Persistence.Records;
using FitTrack.Domain.WorkoutPlanning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitTrack.Adapters.Out.Persistence.Configuration;

internal sealed class WorkoutSeriesRecordConfiguration : IEntityTypeConfiguration<WorkoutSeriesRecord>
{
    public void Configure(EntityTypeBuilder<WorkoutSeriesRecord> builder)
    {
        builder.ToTable("workout_series");

        builder.HasKey(series => series.Id);

        builder.Property(series => series.Name)
            .HasMaxLength(WorkoutSeriesName.MaxLength)
            .IsRequired();

        builder.Property(series => series.AthleteId);

        // A repeated name inside one trainer's list would make the series ambiguous. Two trainers can
        // still share a name, so the author is part of the key.
        builder.HasIndex(series => new { series.TrainerId, series.Name })
            .IsUnique();

        builder.HasMany(series => series.Workouts)
            .WithOne()
            .HasForeignKey(workout => workout.WorkoutSeriesId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
