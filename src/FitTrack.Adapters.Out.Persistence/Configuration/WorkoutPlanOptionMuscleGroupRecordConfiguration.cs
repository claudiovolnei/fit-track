using FitTrack.Adapters.Out.Persistence.Records;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitTrack.Adapters.Out.Persistence.Configuration;

internal sealed class WorkoutPlanOptionMuscleGroupRecordConfiguration
    : IEntityTypeConfiguration<WorkoutPlanOptionMuscleGroupRecord>
{
    public void Configure(EntityTypeBuilder<WorkoutPlanOptionMuscleGroupRecord> builder)
    {
        builder.ToTable("workout_plan_option_muscle_groups");

        builder.HasKey(link => new { link.WorkoutPlanOptionId, link.MuscleGroupId });

        // Restrict: a muscle group that an option points at is not deletable, the same rule the
        // exercises already follow.
        builder.HasOne<MuscleGroupRecord>()
            .WithMany()
            .HasForeignKey(link => link.MuscleGroupId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
