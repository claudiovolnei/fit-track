using FitTrack.Adapters.Out.Persistence.Records;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitTrack.Adapters.Out.Persistence.Configuration;

internal sealed class PerformedSetRecordConfiguration : IEntityTypeConfiguration<PerformedSetRecord>
{
    public void Configure(EntityTypeBuilder<PerformedSetRecord> builder)
    {
        builder.ToTable("performed_sets");

        builder.HasKey(set => new { set.WorkoutSessionId, set.Position });

        builder.Property(set => set.Position)
            .IsRequired();

        builder.Property(set => set.LoadKilograms)
            .HasPrecision(7, 2)
            .IsRequired();

        builder.Property(set => set.Repetitions)
            .IsRequired();
    }
}
