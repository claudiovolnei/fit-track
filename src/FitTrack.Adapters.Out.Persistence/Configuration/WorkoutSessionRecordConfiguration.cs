using FitTrack.Adapters.Out.Persistence.Records;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitTrack.Adapters.Out.Persistence.Configuration;

internal sealed class WorkoutSessionRecordConfiguration : IEntityTypeConfiguration<WorkoutSessionRecord>
{
    public void Configure(EntityTypeBuilder<WorkoutSessionRecord> builder)
    {
        builder.ToTable("workout_sessions");

        builder.HasKey(session => session.Id);

        builder.Property(session => session.WorkoutDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(session => session.StartedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(session => session.FinishedAt)
            .HasColumnType("timestamptz");

        builder.HasIndex(session => session.UserId);

        // One workout per option per day, race-proof. The filter keeps the ongoing workouts started
        // outside a plan (no option) out of the rule.
        builder.HasIndex(session => new { session.UserId, session.WorkoutDate, session.WorkoutPlanOptionId })
            .IsUnique()
            .HasFilter("\"WorkoutPlanOptionId\" IS NOT NULL");

        builder.HasMany(session => session.Sets)
            .WithOne()
            .HasForeignKey(set => set.WorkoutSessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
