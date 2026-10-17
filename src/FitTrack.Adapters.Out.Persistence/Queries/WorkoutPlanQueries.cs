using FitTrack.Adapters.Out.Persistence.Records;
using FitTrack.Application.Dtos;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutPlanning;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Adapters.Out.Persistence.Queries;

/// <summary>
/// Read-side adapter for the plan screens. It loads the pieces with a handful of set-based queries and
/// composes in memory, so the shape of the read model never turns into an N+1 over the options.
/// </summary>
internal sealed class WorkoutPlanQueries(FitTrackDbContext dbContext) : IQueryWorkoutPlans
{
    public async Task<IReadOnlyList<WorkoutPlanDto>> ListByTrainerAsync(
        UserId trainerId,
        CancellationToken cancellationToken)
    {
        var plans = await dbContext.WorkoutPlans
            .AsNoTracking()
            .Where(plan => plan.TrainerId == trainerId.Value)
            .Select(plan => new PlanRow(plan.Id, plan.UserId, plan.TrainerId))
            .ToListAsync(cancellationToken);

        return await ComposeAsync(plans, today: null, now: null, cancellationToken);
    }

    public async Task<WorkoutPlanDto?> GetByIdAsync(WorkoutPlanId planId, CancellationToken cancellationToken)
    {
        var plan = await FindAsync(plan => plan.Id == planId.Value, cancellationToken);

        if (plan is null)
        {
            return null;
        }

        var composed = await ComposeAsync([plan], today: null, now: null, cancellationToken);

        return composed[0];
    }

    public async Task<WorkoutPlanDto?> GetByAthleteAsync(
        UserId userId,
        DateOnly today,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var plan = await FindAsync(candidate => candidate.UserId == userId.Value, cancellationToken);

        if (plan is null)
        {
            return null;
        }

        var composed = await ComposeAsync([plan], today, now, cancellationToken);

        return composed[0];
    }

    private async Task<PlanRow?> FindAsync(
        System.Linq.Expressions.Expression<Func<WorkoutPlanRecord, bool>> predicate,
        CancellationToken cancellationToken) =>
        await dbContext.WorkoutPlans
            .AsNoTracking()
            .Where(predicate)
            .Select(plan => new PlanRow(plan.Id, plan.UserId, plan.TrainerId))
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<IReadOnlyList<WorkoutPlanDto>> ComposeAsync(
        IReadOnlyList<PlanRow> plans,
        DateOnly? today,
        DateTimeOffset? now,
        CancellationToken cancellationToken)
    {
        var planIds = plans.Select(plan => plan.Id).ToList();

        var options = await dbContext.WorkoutPlanOptions
            .AsNoTracking()
            .Where(option => planIds.Contains(option.WorkoutPlanId))
            .OrderBy(option => option.Position)
            .Select(option => new { option.Id, option.WorkoutPlanId, option.Position })
            .ToListAsync(cancellationToken);

        var optionIds = options.Select(option => option.Id).ToList();

        var links = await dbContext.WorkoutPlanOptionMuscleGroups
            .AsNoTracking()
            .Where(link => optionIds.Contains(link.WorkoutPlanOptionId))
            .Join(
                dbContext.MuscleGroups.AsNoTracking(),
                link => link.MuscleGroupId,
                muscleGroup => muscleGroup.Id,
                (link, muscleGroup) => new
                {
                    link.WorkoutPlanOptionId,
                    MuscleGroupId = muscleGroup.Id,
                    MuscleGroupName = muscleGroup.Name,
                })
            .ToListAsync(cancellationToken);

        var todayByOption = today is null || now is null
            ? new Dictionary<Guid, TodayWorkoutDto>()
            : await LoadTodayAsync(
                [.. plans.Select(plan => plan.UserId).Distinct()],
                today.Value,
                now.Value,
                optionIds,
                cancellationToken);

        return
        [
            .. plans.Select(plan => new WorkoutPlanDto(
                plan.Id,
                plan.UserId,
                plan.TrainerId,
                [
                    .. options
                        .Where(option => option.WorkoutPlanId == plan.Id)
                        .OrderBy(option => option.Position)
                        .Select(option => new WorkoutPlanOptionDto(
                            option.Id,
                            option.Position,
                            [
                                .. links
                                    .Where(link => link.WorkoutPlanOptionId == option.Id)
                                    .OrderBy(link => link.MuscleGroupName)
                                    .Select(link => new MuscleGroupDto(link.MuscleGroupId, link.MuscleGroupName)),
                            ],
                            todayByOption.GetValueOrDefault(option.Id))),
                ])),
        ];
    }

    /// <summary>
    /// The workouts already started today, by option. The elapsed seconds are derived here: the
    /// subtraction and the unit conversion are not translatable on every provider.
    /// </summary>
    private async Task<Dictionary<Guid, TodayWorkoutDto>> LoadTodayAsync(
        IReadOnlyList<Guid> userIds,
        DateOnly today,
        DateTimeOffset now,
        IReadOnlyList<Guid> optionIds,
        CancellationToken cancellationToken)
    {
        var sessions = await dbContext.WorkoutSessions
            .AsNoTracking()
            .Include(session => session.Sets)
            .Where(session =>
                userIds.Contains(session.UserId)
                && session.WorkoutDate == today
                && session.WorkoutPlanOptionId != null
                && optionIds.Contains(session.WorkoutPlanOptionId.Value))
            .ToListAsync(cancellationToken);

        return sessions.ToDictionary(
            session => session.WorkoutPlanOptionId!.Value,
            session => new TodayWorkoutDto(
                session.Id,
                session.StartedAt,
                session.FinishedAt,
                (int)((session.FinishedAt ?? now) - session.StartedAt).TotalSeconds,
                session.FinishedAt is null,
                [
                    .. session.Sets
                        .OrderBy(set => set.Position)
                        .Select(set => new PerformedSetDto(
                            set.ExerciseId,
                            set.LoadKilograms,
                            set.Repetitions)),
                ]));
    }

    private sealed record PlanRow(Guid Id, Guid UserId, Guid TrainerId);
}
