using FitTrack.Application.Dtos;
using FitTrack.Application.Ports.Out;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Adapters.Out.Persistence.Queries;

internal sealed class ExerciseQueries(FitTrackDbContext dbContext) : IQueryExercises
{
    public async Task<IReadOnlyList<ExerciseDto>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.Exercises
            .AsNoTracking()
            .Join(
                dbContext.MuscleGroups.AsNoTracking(),
                exercise => exercise.MuscleGroupId,
                muscleGroup => muscleGroup.Id,
                (exercise, muscleGroup) => new { exercise, muscleGroup })
            .OrderBy(row => row.exercise.Name)
            .Select(row => new ExerciseDto(
                row.exercise.Id,
                row.exercise.Name,
                row.muscleGroup.Id,
                row.muscleGroup.Name,
                row.exercise.IsActive))
            .ToListAsync(cancellationToken);
}
