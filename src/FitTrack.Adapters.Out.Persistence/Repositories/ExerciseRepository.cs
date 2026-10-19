using FitTrack.Adapters.Out.Persistence.Mapping;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.WorkoutCatalog;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Adapters.Out.Persistence.Repositories;

internal sealed class ExerciseRepository(FitTrackDbContext dbContext) : IExerciseRepository
{
    public async Task<Exercise?> GetByIdAsync(ExerciseId id, CancellationToken cancellationToken)
    {
        var record = await dbContext.Exercises
            .AsNoTracking()
            .FirstOrDefaultAsync(exercise => exercise.Id == id.Value, cancellationToken);

        return record is null ? null : ExerciseMapper.ToDomain(record);
    }

    public async Task<IReadOnlyList<Exercise>> GetByIdsAsync(
        IReadOnlyList<ExerciseId> ids,
        CancellationToken cancellationToken)
    {
        var wanted = ids.Select(id => id.Value).ToList();

        var records = await dbContext.Exercises
            .AsNoTracking()
            .Where(exercise => wanted.Contains(exercise.Id))
            .ToListAsync(cancellationToken);

        return [.. records.Select(ExerciseMapper.ToDomain)];
    }

    public async Task SaveAsync(Exercise exercise, CancellationToken cancellationToken)
    {
        var record = await dbContext.Exercises
            .FirstOrDefaultAsync(candidate => candidate.Id == exercise.Id.Value, cancellationToken);

        if (record is null)
        {
            dbContext.Exercises.Add(ExerciseMapper.ToRecord(exercise));
        }
        else
        {
            ExerciseMapper.CopyInto(exercise, record);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
