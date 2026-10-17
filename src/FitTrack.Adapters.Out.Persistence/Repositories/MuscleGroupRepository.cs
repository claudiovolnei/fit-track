using FitTrack.Adapters.Out.Persistence.Mapping;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.WorkoutCatalog;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Adapters.Out.Persistence.Repositories;

internal sealed class MuscleGroupRepository(FitTrackDbContext dbContext) : IMuscleGroupRepository
{
    public async Task<MuscleGroup?> GetByIdAsync(MuscleGroupId id, CancellationToken cancellationToken)
    {
        var record = await dbContext.MuscleGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(muscleGroup => muscleGroup.Id == id.Value, cancellationToken);

        return record is null ? null : MuscleGroupMapper.ToDomain(record);
    }

    public async Task<IReadOnlyList<MuscleGroup>> GetByIdsAsync(
        IReadOnlyList<MuscleGroupId> ids,
        CancellationToken cancellationToken)
    {
        var wanted = ids.Select(id => id.Value).ToList();

        var records = await dbContext.MuscleGroups
            .AsNoTracking()
            .Where(muscleGroup => wanted.Contains(muscleGroup.Id))
            .ToListAsync(cancellationToken);

        return [.. records.Select(MuscleGroupMapper.ToDomain)];
    }

    public async Task<bool> NameTakenAsync(
        MuscleGroupName name,
        MuscleGroupId? excluding,
        CancellationToken cancellationToken)
    {
        var excluded = excluding?.Value;

        return await dbContext.MuscleGroups
            .AnyAsync(
                candidate => candidate.Name == name.Value && (excluded == null || candidate.Id != excluded),
                cancellationToken);
    }

    public async Task SaveAsync(MuscleGroup muscleGroup, CancellationToken cancellationToken)
    {
        var record = await dbContext.MuscleGroups
            .FirstOrDefaultAsync(candidate => candidate.Id == muscleGroup.Id.Value, cancellationToken);

        if (record is null)
        {
            dbContext.MuscleGroups.Add(MuscleGroupMapper.ToRecord(muscleGroup));
        }
        else
        {
            MuscleGroupMapper.CopyInto(muscleGroup, record);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
