using FitTrack.Adapters.Out.Persistence.Records;
using FitTrack.Domain.WorkoutCatalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitTrack.Adapters.Out.Persistence.Configuration;

internal sealed class ExerciseRecordConfiguration : IEntityTypeConfiguration<ExerciseRecord>
{
    public void Configure(EntityTypeBuilder<ExerciseRecord> builder)
    {
        builder.ToTable("exercises");

        builder.HasKey(exercise => exercise.Id);

        builder.Property(exercise => exercise.Name)
            .HasMaxLength(ExerciseName.MaxLength)
            .IsRequired();

        // Same context, so a real foreign key: a muscle group in use cannot be deleted.
        builder.HasOne<MuscleGroupRecord>()
            .WithMany()
            .HasForeignKey(exercise => exercise.MuscleGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(exercise => exercise.MuscleGroupId);
    }
}
