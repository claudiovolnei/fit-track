using FitTrack.Application.Dtos;
using FitTrack.Application.Ports.Out;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Adapters.Out.Persistence.Queries;

internal sealed class MuscleGroupQueries(FitTrackDbContext dbContext) : IQueryMuscleGroups
{
    public async Task<IReadOnlyList<MuscleGroupDto>> ListAsync(CancellationToken cancellationToken) =>
        (await ListAsync(ListQuery.All, cancellationToken)).Items;

    public async Task<PagedResult<MuscleGroupDto>> ListAsync(
        ListQuery query,
        CancellationToken cancellationToken)
    {
        var groups = dbContext.MuscleGroups.AsNoTracking();

        if (query.Search is { } search)
        {
            var pattern = SearchPattern.Contains(search);
            groups = groups.Where(group => EF.Functions.ILike(group.Name, pattern, "\\"));
        }

        var total = await groups.CountAsync(cancellationToken);

        var ordered = groups
            .OrderBy(group => group.Name)
            .ThenBy(group => group.Id);

        var page = query.IsPaged
            ? ordered.Skip(query.Skip).Take(query.Take)
            : ordered;

        var items = await page
            .Select(group => new MuscleGroupDto(group.Id, group.Name))
            .ToListAsync(cancellationToken);

        return new PagedResult<MuscleGroupDto>(items, query.Page, query.PageSize, total);
    }
}
