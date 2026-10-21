using FitTrack.Application.Ports.Out;
using FitTrack.Domain.WorkoutCatalog;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Adapters.Out.Persistence.Queries;

/// <summary>
/// Reads the catalog usage that guards deletion across bounded contexts: a prescribed exercise
/// lives in workout planning (series workouts) and an executed one in workout execution (session
/// sets). Both stop an exercise from leaving the catalog while any workout still names it.
/// </summary>
internal sealed class CatalogUsageQueries(FitTrackDbContext dbContext) : ICheckCatalogUsage
{
    public async Task<bool> ExerciseReferencedAsync(ExerciseId exerciseId, CancellationToken cancellationToken)
    {
        var exerciseIdValue = exerciseId.Value;

        var prescribed = await dbContext.WorkoutSeriesWorkoutExercises
            .AsNoTracking()
            .AnyAsync(prescription => prescription.ExerciseId == exerciseIdValue, cancellationToken);

        if (prescribed)
        {
            return true;
        }

        return await dbContext.WorkoutSessions
            .AsNoTracking()
            .AnyAsync(
                session => session.Sets.Any(set => set.ExerciseId == exerciseIdValue),
                cancellationToken);
    }

    public Task<bool> MuscleGroupHasExercisesAsync(MuscleGroupId muscleGroupId, CancellationToken cancellationToken) =>
        dbContext.Exercises
            .AsNoTracking()
            .AnyAsync(exercise => exercise.MuscleGroupId == muscleGroupId.Value, cancellationToken);
}
