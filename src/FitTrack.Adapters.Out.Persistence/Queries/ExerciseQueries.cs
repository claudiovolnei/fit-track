using FitTrack.Application.Dtos;
using FitTrack.Application.Ports.Out;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Adapters.Out.Persistence.Queries;

internal sealed class ExerciseQueries(FitTrackDbContext dbContext) : IQueryExercises
{
    public async Task<IReadOnlyList<ExerciseDto>> ListAsync(CancellationToken cancellationToken) =>
        (await ListAsync(ListQuery.All, muscleGroupId: null, cancellationToken)).Items;

    public async Task<PagedResult<ExerciseDto>> ListAsync(
        ListQuery query,
        Guid? muscleGroupId,
        CancellationToken cancellationToken)
    {
        var rows = dbContext.Exercises
            .AsNoTracking()
            .Join(
                dbContext.MuscleGroups.AsNoTracking(),
                exercise => exercise.MuscleGroupId,
                muscleGroup => muscleGroup.Id,
                (exercise, muscleGroup) => new { exercise, muscleGroup });

        if (muscleGroupId is { } groupId && groupId != Guid.Empty)
        {
            rows = rows.Where(row => row.muscleGroup.Id == groupId);
        }

        if (query.Search is { } search)
        {
            var pattern = SearchPattern.Contains(search);
            var escape = "\\";

            rows = rows.Where(row =>
                EF.Functions.ILike(row.exercise.Name, pattern, escape)
                || EF.Functions.ILike(row.muscleGroup.Name, pattern, escape));
        }

        var total = await rows.CountAsync(cancellationToken);

        var ordered = rows
            .OrderBy(row => row.exercise.Name)
            .ThenBy(row => row.exercise.Id);

        var page = query.IsPaged
            ? ordered.Skip(query.Skip).Take(query.Take)
            : ordered;

        var items = await page
            .Select(row => new ExerciseDto(
                row.exercise.Id,
                row.exercise.Name,
                row.muscleGroup.Id,
                row.muscleGroup.Name,
                row.exercise.IsActive))
            .ToListAsync(cancellationToken);

        return new PagedResult<ExerciseDto>(items, query.Page, query.PageSize, total);
    }
}
