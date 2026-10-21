using FitTrack.Adapters.Out.Persistence.Mapping;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Adapters.Out.Persistence.Repositories;

internal sealed class TrainerAthleteAssignmentRepository(FitTrackDbContext dbContext)
    : ITrainerAthleteAssignmentRepository
{
    public async Task<TrainerAthleteAssignment?> GetByAthleteIdAsync(
        UserId athleteId,
        CancellationToken cancellationToken)
    {
        var record = await dbContext.TrainerAthleteAssignments
            .AsNoTracking()
            .FirstOrDefaultAsync(assignment => assignment.AthleteId == athleteId.Value, cancellationToken);

        return record is null ? null : TrainerAthleteAssignmentMapper.ToDomain(record);
    }

    public async Task SaveAsync(TrainerAthleteAssignment assignment, CancellationToken cancellationToken)
    {
        var exists = await dbContext.TrainerAthleteAssignments
            .AnyAsync(candidate => candidate.Id == assignment.Id.Value, cancellationToken);

        if (!exists)
        {
            dbContext.TrainerAthleteAssignments.Add(TrainerAthleteAssignmentMapper.ToRecord(assignment));
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RemoveAsync(TrainerAthleteAssignment assignment, CancellationToken cancellationToken)
    {
        var record = await dbContext.TrainerAthleteAssignments
            .FirstOrDefaultAsync(candidate => candidate.Id == assignment.Id.Value, cancellationToken);

        if (record is not null)
        {
            dbContext.TrainerAthleteAssignments.Remove(record);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
