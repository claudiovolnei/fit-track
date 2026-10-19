using FitTrack.Adapters.Out.Persistence.Mapping;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutPlanning;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Adapters.Out.Persistence.Repositories;

internal sealed class WorkoutSeriesRepository(FitTrackDbContext dbContext) : IWorkoutSeriesRepository
{
    public async Task<WorkoutSeries?> GetByIdAsync(WorkoutSeriesId id, CancellationToken cancellationToken)
    {
        var record = await dbContext.WorkoutSeries
            .AsNoTracking()
            .Include(series => series.Workouts)
            .ThenInclude(workout => workout.Exercises)
            .FirstOrDefaultAsync(series => series.Id == id.Value, cancellationToken);

        return record is null ? null : WorkoutSeriesMapper.ToDomain(record);
    }

    public async Task<WorkoutSeriesWorkout?> GetWorkoutAsync(
        WorkoutSeriesWorkoutId id,
        CancellationToken cancellationToken)
    {
        var seriesId = await dbContext.WorkoutSeriesWorkouts
            .AsNoTracking()
            .Where(workout => workout.Id == id.Value)
            .Select(workout => (Guid?)workout.WorkoutSeriesId)
            .FirstOrDefaultAsync(cancellationToken);

        if (seriesId is null)
        {
            return null;
        }

        // The workout comes through its series: position and label are invariants of the aggregate, so
        // the read reuses the same mapper the rest of the write side already uses.
        var series = await GetByIdAsync(new WorkoutSeriesId(seriesId.Value), cancellationToken);

        return series?.Workouts.FirstOrDefault(workout => workout.Id == id);
    }

    public Task<bool> NameTakenAsync(
        WorkoutSeriesName name,
        UserId trainerId,
        WorkoutSeriesId? excluding,
        CancellationToken cancellationToken)
    {
        var excluded = excluding?.Value;

        return dbContext.WorkoutSeries
            .AnyAsync(
                series => series.TrainerId == trainerId.Value
                    && series.Name == name.Value
                    && (excluded == null || series.Id != excluded),
                cancellationToken);
    }

    public async Task SaveAsync(WorkoutSeries series, CancellationToken cancellationToken)
    {
        var record = await dbContext.WorkoutSeries
            .Include(candidate => candidate.Workouts)
            .ThenInclude(workout => workout.Exercises)
            .FirstOrDefaultAsync(candidate => candidate.Id == series.Id.Value, cancellationToken);

        if (record is null)
        {
            dbContext.WorkoutSeries.Add(WorkoutSeriesMapper.ToRecord(series));
        }
        else
        {
            // The rows that are not stored yet go through the sets, which states their state explicitly:
            // EF would otherwise track a key-set entity discovered in a navigation as Modified.
            var changes = WorkoutSeriesMapper.CopyInto(series, record);

            if (changes.Workouts.Count > 0)
            {
                dbContext.WorkoutSeriesWorkouts.AddRange(changes.Workouts);
            }

            if (changes.Exercises.Count > 0)
            {
                dbContext.WorkoutSeriesWorkoutExercises.AddRange(changes.Exercises);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
