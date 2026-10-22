using FitTrack.Adapters.Out.Persistence.Records;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitTrack.Adapters.Out.Persistence.Configuration;

internal sealed class WorkoutSeriesAssignmentRecordConfiguration
    : IEntityTypeConfiguration<WorkoutSeriesAssignmentRecord>
{
    public void Configure(EntityTypeBuilder<WorkoutSeriesAssignmentRecord> builder)
    {
        builder.ToTable("workout_series_assignments");

        builder.HasKey(assignment => assignment.Id);

        builder.Property(assignment => assignment.AssignedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(assignment => assignment.ExpiresAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        // A cycle the trainer closed early: null while the athlete follows the series.
        builder.Property(assignment => assignment.ClosedAt)
            .HasColumnType("timestamptz");

        // One series per athlete: the unique index is the rule, and the use case checks first so the
        // athlete gets a stated answer instead of a raw index error.
        builder.HasIndex(assignment => assignment.UserId)
            .IsUnique();

        // The expiry is read on every athlete screen, so it is worth its own index.
        builder.HasIndex(assignment => assignment.ExpiresAt);

        // Restrict (not cascade): dropping a series out from under an athlete would erase their link
        // silently. The trainer reassigns instead.
        builder.HasOne<WorkoutSeriesRecord>()
            .WithMany()
            .HasForeignKey(assignment => assignment.WorkoutSeriesId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
