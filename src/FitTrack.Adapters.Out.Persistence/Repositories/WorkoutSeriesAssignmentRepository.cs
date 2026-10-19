using FitTrack.Adapters.Out.Persistence.Mapping;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutPlanning;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Adapters.Out.Persistence.Repositories;

internal sealed class WorkoutSeriesAssignmentRepository(FitTrackDbContext dbContext)
    : IWorkoutSeriesAssignmentRepository
{
    public async Task<WorkoutSeriesAssignment?> GetByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken)
    {
        var record = await dbContext.WorkoutSeriesAssignments
            .AsNoTracking()
            .FirstOrDefaultAsync(assignment => assignment.UserId == userId.Value, cancellationToken);

        return record is null ? null : WorkoutSeriesAssignmentMapper.ToDomain(record);
    }

    public async Task SaveAsync(WorkoutSeriesAssignment assignment, CancellationToken cancellationToken)
    {
        var record = await dbContext.WorkoutSeriesAssignments
            .FirstOrDefaultAsync(candidate => candidate.Id == assignment.Id.Value, cancellationToken);

        if (record is null)
        {
            dbContext.WorkoutSeriesAssignments.Add(WorkoutSeriesAssignmentMapper.ToRecord(assignment));
        }
        else
        {
            WorkoutSeriesAssignmentMapper.CopyInto(assignment, record);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
