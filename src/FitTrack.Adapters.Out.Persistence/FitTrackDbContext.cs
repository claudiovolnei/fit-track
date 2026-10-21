using FitTrack.Adapters.Out.Persistence.Accounts;
using FitTrack.Adapters.Out.Persistence.Records;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Adapters.Out.Persistence;

/// <summary>
/// Single EF Core context for the outbound adapter. It holds Identity tables plus the
/// persistence records that mirror the domain aggregates; the records are mapped to
/// aggregates by the mappers in this assembly.
/// </summary>
public sealed class FitTrackDbContext(DbContextOptions<FitTrackDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<ExerciseRecord> Exercises => Set<ExerciseRecord>();

    public DbSet<MuscleGroupRecord> MuscleGroups => Set<MuscleGroupRecord>();

    public DbSet<WorkoutSessionRecord> WorkoutSessions => Set<WorkoutSessionRecord>();

    public DbSet<WorkoutSeriesRecord> WorkoutSeries => Set<WorkoutSeriesRecord>();

    public DbSet<WorkoutSeriesWorkoutRecord> WorkoutSeriesWorkouts => Set<WorkoutSeriesWorkoutRecord>();

    public DbSet<WorkoutSeriesWorkoutExerciseRecord> WorkoutSeriesWorkoutExercises =>
        Set<WorkoutSeriesWorkoutExerciseRecord>();

    public DbSet<WorkoutSeriesAssignmentRecord> WorkoutSeriesAssignments =>
        Set<WorkoutSeriesAssignmentRecord>();

    public DbSet<TrainerAthleteAssignmentRecord> TrainerAthleteAssignments =>
        Set<TrainerAthleteAssignmentRecord>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(FitTrackDbContext).Assembly);
    }
}
