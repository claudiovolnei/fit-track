using FitTrack.Adapters.Out.Persistence.Mapping;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutExecution;
using FitTrack.Domain.WorkoutPlanning;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Adapters.Out.Persistence.Repositories;

internal sealed class WorkoutSessionRepository(FitTrackDbContext dbContext) : IWorkoutSessionRepository
{
    public async Task<WorkoutSession?> GetByIdAsync(WorkoutSessionId id, CancellationToken cancellationToken)
    {
        var record = await dbContext.WorkoutSessions
            .AsNoTracking()
            .Include(session => session.Sets)
            .Include(session => session.CompletedExercises)
            .FirstOrDefaultAsync(session => session.Id == id.Value, cancellationToken);

        return record is null ? null : WorkoutSessionMapper.ToDomain(record);
    }

    public async Task<WorkoutSession?> GetByPlannedWorkoutAsync(
        UserId userId,
        DateOnly workoutDate,
        WorkoutSeriesWorkoutId workoutId,
        CancellationToken cancellationToken)
    {
        var record = await dbContext.WorkoutSessions
            .AsNoTracking()
            .Include(session => session.Sets)
            .Include(session => session.CompletedExercises)
            .FirstOrDefaultAsync(
                session => session.UserId == userId.Value
                    && session.WorkoutDate == workoutDate
                    && session.WorkoutSeriesWorkoutId == workoutId.Value,
                cancellationToken);

        return record is null ? null : WorkoutSessionMapper.ToDomain(record);
    }

    public async Task<IReadOnlyList<WorkoutSession>> ListByDateAsync(
        UserId userId,
        DateOnly workoutDate,
        CancellationToken cancellationToken)
    {
        var records = await dbContext.WorkoutSessions
            .AsNoTracking()
            .Include(session => session.Sets)
            .Include(session => session.CompletedExercises)
            .Where(session => session.UserId == userId.Value && session.WorkoutDate == workoutDate)
            .ToListAsync(cancellationToken);

        return [.. records.Select(WorkoutSessionMapper.ToDomain)];
    }

    public async Task<IReadOnlyList<WorkoutSession>> ListRunningStartedBeforeAsync(
        DateTimeOffset cutoff,
        CancellationToken cancellationToken)
    {
        var records = await dbContext.WorkoutSessions
            .AsNoTracking()
            .Include(session => session.Sets)
            .Include(session => session.CompletedExercises)
            .Where(session => session.FinishedAt == null && session.StartedAt <= cutoff)
            .ToListAsync(cancellationToken);

        return [.. records.Select(WorkoutSessionMapper.ToDomain)];
    }

    public async Task<IReadOnlyList<WorkoutSession>> ListRunningStartedBeforeAsync(
        UserId userId,
        DateTimeOffset cutoff,
        CancellationToken cancellationToken)
    {
        var records = await dbContext.WorkoutSessions
            .AsNoTracking()
            .Include(session => session.Sets)
            .Include(session => session.CompletedExercises)
            .Where(session => session.UserId == userId.Value && session.FinishedAt == null && session.StartedAt <= cutoff)
            .ToListAsync(cancellationToken);

        return [.. records.Select(WorkoutSessionMapper.ToDomain)];
    }

    public async Task SaveAsync(WorkoutSession session, CancellationToken cancellationToken)
    {
        var record = await dbContext.WorkoutSessions
            .Include(candidate => candidate.Sets)
            .Include(candidate => candidate.CompletedExercises)
            .FirstOrDefaultAsync(candidate => candidate.Id == session.Id.Value, cancellationToken);

        if (record is null)
        {
            dbContext.WorkoutSessions.Add(WorkoutSessionMapper.ToRecord(session));
        }
        else
        {
            if (record.FinishedAt is not null)
            {
                // Already finished in the database (e.g. by auto-finish). Do not overwrite.
                return;
            }

            WorkoutSessionMapper.CopyInto(session, record);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(WorkoutSession session, CancellationToken cancellationToken)
    {
        var record = await dbContext.WorkoutSessions
            .FirstOrDefaultAsync(candidate => candidate.Id == session.Id.Value, cancellationToken);

        if (record is null)
        {
            return;
        }

        if (record.FinishedAt is not null)
        {
            // Never remove an already finished session.
            return;
        }

        dbContext.WorkoutSessions.Remove(record);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
