using FitTrack.Adapters.Out.Persistence.Records;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitTrack.Adapters.Out.Persistence.Configuration;

internal sealed class WorkoutPlanRecordConfiguration : IEntityTypeConfiguration<WorkoutPlanRecord>
{
    public void Configure(EntityTypeBuilder<WorkoutPlanRecord> builder)
    {
        builder.ToTable("workout_plans");

        builder.HasKey(plan => plan.Id);

        builder.HasIndex(plan => plan.UserId).IsUnique();
        builder.HasIndex(plan => plan.TrainerId);

        builder.HasMany(plan => plan.Options)
            .WithOne()
            .HasForeignKey(option => option.WorkoutPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        // The athlete and the author are accounts of the Identity context, so they stay as ids.
    }
}
