using FitTrack.Application.Dtos;
using FitTrack.Application.Ports.Out;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Adapters.Out.Persistence.Queries;

internal sealed class MuscleGroupQueries(FitTrackDbContext dbContext) : IQueryMuscleGroups
{
    public async Task<IReadOnlyList<MuscleGroupDto>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.MuscleGroups
            .AsNoTracking()
            .OrderBy(muscleGroup => muscleGroup.Name)
            .Select(muscleGroup => new MuscleGroupDto(muscleGroup.Id, muscleGroup.Name))
            .ToListAsync(cancellationToken);
}
