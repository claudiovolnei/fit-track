using FitTrack.Adapters.Out.Persistence.Records;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitTrack.Adapters.Out.Persistence.Configuration;

internal sealed class TrainerAthleteAssignmentRecordConfiguration
    : IEntityTypeConfiguration<TrainerAthleteAssignmentRecord>
{
    public void Configure(EntityTypeBuilder<TrainerAthleteAssignmentRecord> builder)
    {
        builder.ToTable("trainer_athlete_assignments");

        builder.HasKey(assignment => assignment.Id);

        builder.Property(assignment => assignment.LinkedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        // One trainer per athlete: the unique index is the rule, and the use case checks first so the
        // caller gets a stated answer instead of a raw index error.
        builder.HasIndex(assignment => assignment.AthleteId)
            .IsUnique();

        // The roster is read on every athlete screen, so it is worth its own index.
        builder.HasIndex(assignment => assignment.TrainerId);
    }
}
