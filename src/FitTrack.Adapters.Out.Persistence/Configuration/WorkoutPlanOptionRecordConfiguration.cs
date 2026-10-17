using FitTrack.Adapters.Out.Persistence.Records;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitTrack.Adapters.Out.Persistence.Configuration;

internal sealed class WorkoutPlanOptionRecordConfiguration : IEntityTypeConfiguration<WorkoutPlanOptionRecord>
{
    public void Configure(EntityTypeBuilder<WorkoutPlanOptionRecord> builder)
    {
        builder.ToTable("workout_plan_options");

        builder.HasKey(option => option.Id);

        // Not unique on purpose: removing an option renumbers the rest, and a unique index would make
        // the renumbering order matter. The aggregate is the one that keeps positions contiguous.
        builder.HasIndex(option => new { option.WorkoutPlanId, option.Position });

        builder.HasMany(option => option.MuscleGroups)
            .WithOne()
            .HasForeignKey(link => link.WorkoutPlanOptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
