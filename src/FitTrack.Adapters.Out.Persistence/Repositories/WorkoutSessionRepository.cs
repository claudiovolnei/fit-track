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
            .FirstOrDefaultAsync(session => session.Id == id.Value, cancellationToken);

        return record is null ? null : WorkoutSessionMapper.ToDomain(record);
    }

    public async Task<WorkoutSession?> GetByPlannedWorkoutAsync(
        UserId userId,
        DateOnly workoutDate,
        WorkoutPlanOptionId optionId,
        CancellationToken cancellationToken)
    {
        var record = await dbContext.WorkoutSessions
            .AsNoTracking()
            .Include(session => session.Sets)
            .FirstOrDefaultAsync(
                session => session.UserId == userId.Value
                    && session.WorkoutDate == workoutDate
                    && session.WorkoutPlanOptionId == optionId.Value,
                cancellationToken);

        return record is null ? null : WorkoutSessionMapper.ToDomain(record);
    }

    public async Task SaveAsync(WorkoutSession session, CancellationToken cancellationToken)
    {
        var record = await dbContext.WorkoutSessions
            .Include(candidate => candidate.Sets)
            .FirstOrDefaultAsync(candidate => candidate.Id == session.Id.Value, cancellationToken);

        if (record is null)
        {
            dbContext.WorkoutSessions.Add(WorkoutSessionMapper.ToRecord(session));
        }
        else
        {
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

        dbContext.WorkoutSessions.Remove(record);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
