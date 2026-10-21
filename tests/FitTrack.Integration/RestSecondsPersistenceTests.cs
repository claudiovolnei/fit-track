using FitTrack.Adapters.Out.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Integration;

/// <summary>
/// Reads the <c>RestSeconds</c> column straight from PostgreSQL, so the round trip through the API
/// cannot hide what the database actually holds. The point is the value 0: the column has a database
/// default of 60, and the EF sentinel is what keeps an explicit 0 from being swallowed by it — a
/// prescription of "no rest" has to be stored as 0, not as 60.
/// </summary>
public sealed class RestSecondsPersistenceTests(FitTrackApiFactory factory) : IClassFixture<FitTrackApiFactory>
{
    [RequiresPostgresFact]
    public async Task TheColumnHoldsZeroAndNotTheDatabaseDefault()
    {
        var trainer = await factory.TrainerAsync();
        var exercise = await SeriesArrange.NewExerciseAsync(trainer, "Coluna Descanso");

        var series = await trainer.CreateSeriesAsync($"Coluna {Guid.NewGuid():N}");

        await trainer.AddWorkoutAsync(series.Id, "Omitted", Prescriptions.For(exercise.Id));
        await trainer.AddWorkoutAsync(series.Id, "Zero", Prescriptions.For(exercise.Id, restSeconds: 0));
        await trainer.AddWorkoutAsync(series.Id, "Noventa", Prescriptions.For(exercise.Id, restSeconds: 90));

        var connectionString = Environment.GetEnvironmentVariable(
            RequiresPostgresFactAttribute.ConnectionStringVariable)!;

        var options = new DbContextOptionsBuilder<FitTrackDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var db = new FitTrackDbContext(options);

        var workouts = await db.WorkoutSeriesWorkouts.AsNoTracking()
            .Where(workout => workout.WorkoutSeriesId == series.Id)
            .OrderBy(workout => workout.Position)
            .Select(workout => new { workout.Id, workout.Name })
            .ToListAsync();

        var workoutIds = workouts.Select(workout => workout.Id).ToList();

        var prescriptions = await db.WorkoutSeriesWorkoutExercises.AsNoTracking()
            .Where(row => workoutIds.Contains(row.WorkoutSeriesWorkoutId))
            .Select(row => new { row.WorkoutSeriesWorkoutId, row.RestSeconds })
            .ToListAsync();

        var stored = workouts
            .Select(workout => (
                workout.Name,
                prescriptions.Single(row => row.WorkoutSeriesWorkoutId == workout.Id).RestSeconds))
            .ToArray();

        Assert.Equal(
            new[] { ("Omitted", 60), ("Zero", 0), ("Noventa", 90) },
            stored);
    }
}
