using FitTrack.Adapters.Out.Persistence.Records;
using FitTrack.Domain.WorkoutCatalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitTrack.Adapters.Out.Persistence.Configuration;

internal sealed class MuscleGroupRecordConfiguration : IEntityTypeConfiguration<MuscleGroupRecord>
{
    public void Configure(EntityTypeBuilder<MuscleGroupRecord> builder)
    {
        builder.ToTable("muscle_groups");

        builder.HasKey(muscleGroup => muscleGroup.Id);

        builder.Property(muscleGroup => muscleGroup.Name)
            .HasMaxLength(MuscleGroupName.MaxLength)
            .IsRequired();

        // A repeated name would make the catalog ambiguous, so the database refuses it.
        builder.HasIndex(muscleGroup => muscleGroup.Name)
            .IsUnique();
    }
}
