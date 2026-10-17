using System.Linq.Expressions;
using FitTrack.Adapters.Out.Persistence.Mapping;
using FitTrack.Adapters.Out.Persistence.Records;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutPlanning;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Adapters.Out.Persistence.Repositories;

internal sealed class WorkoutPlanRepository(FitTrackDbContext dbContext) : IWorkoutPlanRepository
{
    public Task<WorkoutPlan?> GetByIdAsync(WorkoutPlanId id, CancellationToken cancellationToken) =>
        LoadAsync(plan => plan.Id == id.Value, cancellationToken);

    public Task<WorkoutPlan?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken) =>
        LoadAsync(plan => plan.UserId == userId.Value, cancellationToken);

    public Task<WorkoutPlan?> GetByOptionIdAsync(
        WorkoutPlanOptionId optionId,
        CancellationToken cancellationToken) =>
        LoadAsync(
            plan => plan.Options.Any(option => option.Id == optionId.Value),
            cancellationToken);

    public async Task SaveAsync(WorkoutPlan plan, CancellationToken cancellationToken)
    {
        var record = await dbContext.WorkoutPlans
            .Include(candidate => candidate.Options)
            .ThenInclude(option => option.MuscleGroups)
            .FirstOrDefaultAsync(candidate => candidate.Id == plan.Id.Value, cancellationToken);

        if (record is null)
        {
            dbContext.WorkoutPlans.Add(WorkoutPlanMapper.ToRecord(plan));
        }
        else
        {
            // The rows that are not stored yet go through the sets, which states their state explicitly:
            // EF would otherwise track a key-set entity discovered in a navigation as Modified.
            var changes = WorkoutPlanMapper.CopyInto(plan, record);

            if (changes.Options.Count > 0)
            {
                dbContext.WorkoutPlanOptions.AddRange(changes.Options);
            }

            if (changes.Links.Count > 0)
            {
                dbContext.WorkoutPlanOptionMuscleGroups.AddRange(changes.Links);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<WorkoutPlan?> LoadAsync(
        Expression<Func<WorkoutPlanRecord, bool>> predicate,
        CancellationToken cancellationToken)
    {
        var record = await dbContext.WorkoutPlans
            .AsNoTracking()
            .Include(plan => plan.Options)
            .ThenInclude(option => option.MuscleGroups)
            .FirstOrDefaultAsync(predicate, cancellationToken);

        return record is null ? null : WorkoutPlanMapper.ToDomain(record);
    }
}
