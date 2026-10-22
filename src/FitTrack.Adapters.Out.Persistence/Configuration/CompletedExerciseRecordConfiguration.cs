using FitTrack.Adapters.Out.Persistence.Records;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitTrack.Adapters.Out.Persistence.Configuration;

internal sealed class CompletedExerciseRecordConfiguration : IEntityTypeConfiguration<CompletedExerciseRecord>
{
    public void Configure(EntityTypeBuilder<CompletedExerciseRecord> builder)
    {
        builder.ToTable("completed_exercises");

        builder.HasKey(completed => new { completed.WorkoutSessionId, completed.ExerciseId });

        builder.Property(completed => completed.ExerciseId)
            .IsRequired();
    }
}
