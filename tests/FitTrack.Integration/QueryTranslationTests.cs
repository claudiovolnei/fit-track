using FitTrack.Adapters.Out.Persistence;
using FitTrack.Adapters.Out.Persistence.Queries;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Integration;

/// <summary>
/// Proves the outbound read queries translate to SQL for the Npgsql provider. It needs no
/// running database: <c>ToQueryString</c> builds the statement without opening a connection,
/// so a broken projection fails here instead of in production.
/// </summary>
public sealed class QueryTranslationTests
{
    [Fact]
    public void WorkoutSessionProjection_TranslatesToSqlOverBothTables()
    {
        using var dbContext = CreateDbContext();

        var sql = dbContext.WorkoutSessions
            .Where(session => session.UserId == Guid.NewGuid())
            .ToWorkoutSessionRows()
            .ToQueryString();

        Assert.Contains("workout_sessions", sql);
        Assert.Contains("performed_sets", sql);
    }

    [Fact]
    public void ExerciseCatalogQuery_TranslatesToSqlOverBothTables()
    {
        using var dbContext = CreateDbContext();

        var sql = dbContext.Exercises
            .Join(
                dbContext.MuscleGroups,
                exercise => exercise.MuscleGroupId,
                muscleGroup => muscleGroup.Id,
                (exercise, muscleGroup) => new { exercise, muscleGroup })
            .OrderBy(row => row.exercise.Name)
            .Select(row => new
            {
                row.exercise.Id,
                row.exercise.Name,
                MuscleGroupId = row.muscleGroup.Id,
                MuscleGroupName = row.muscleGroup.Name,
                row.exercise.IsActive,
            })
            .ToQueryString();

        Assert.Contains("exercises", sql);
        Assert.Contains("muscle_groups", sql);
    }

    [Fact]
    public void ExerciseSearch_TranslatesToCaseInsensitiveIlike()
    {
        using var dbContext = CreateDbContext();

        var pattern = SearchPattern.Contains("Supino 50%");
        var sql = dbContext.Exercises
            .Where(exercise => EF.Functions.ILike(exercise.Name, pattern, "\\"))
            .ToQueryString();

        Assert.Contains("ILIKE", sql);
        Assert.Contains("ESCAPE", sql);
    }

    [Fact]
    public void SessionSearch_TranslatesTheWorkoutNameJoin()
    {
        using var dbContext = CreateDbContext();

        var sql = dbContext.WorkoutSessions
            .Where(session =>
                session.WorkoutSeriesWorkoutId != null
                && dbContext.WorkoutSeriesWorkouts.Any(workout =>
                    workout.Id == session.WorkoutSeriesWorkoutId.Value
                    && EF.Functions.ILike(workout.Name, "%peito%", "\\")))
            .ToQueryString();

        Assert.Contains("workout_series_workouts", sql);
        Assert.Contains("ILIKE", sql);
    }

    private static FitTrackDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<FitTrackDbContext>()
            .UseNpgsql("Host=localhost;Database=translation-only;Username=unused;Password=unused")
            .Options);
}
