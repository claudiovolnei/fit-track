using FitTrack.Application.Dtos;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Unit.Fakes;

/// <summary>
/// Read-side fake for the plan screens. It projects the plans the write fake holds, the same way the
/// SQL adapter projects the saved rows, so the unit test asserts the aggregate and not a copy of it.
/// </summary>
internal sealed class InMemoryWorkoutPlanQueries(
    InMemoryWorkoutPlanRepository plans,
    InMemoryMuscleGroupRepository muscleGroups,
    InMemoryWorkoutSessionRepository sessions) : IQueryWorkoutPlans
{
    public Task<IReadOnlyList<WorkoutPlanDto>> ListByTrainerAsync(
        UserId trainerId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<WorkoutPlanDto>>(
            [.. plans.All.Where(plan => plan.TrainerId == trainerId).Select(ToDto)]);

    public Task<WorkoutPlanDto?> GetByIdAsync(WorkoutPlanId planId, CancellationToken cancellationToken) =>
        Task.FromResult(plans.All.Where(plan => plan.Id == planId).Select(ToDto).FirstOrDefault());

    public Task<WorkoutPlanDto?> GetByAthleteAsync(
        UserId userId,
        DateOnly today,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        Task.FromResult(plans.All
            .Where(plan => plan.UserId == userId)
            .Select(plan => ToDto(plan, today, now))
            .FirstOrDefault());

    private WorkoutPlanDto ToDto(WorkoutPlan plan) =>
        new(
            plan.Id.Value,
            plan.UserId.Value,
            plan.TrainerId.Value,
            [.. plan.Options.Select(option => ToDto(option, null, null))]);

    private WorkoutPlanDto ToDto(WorkoutPlan plan, DateOnly today, DateTimeOffset now) =>
        new(
            plan.Id.Value,
            plan.UserId.Value,
            plan.TrainerId.Value,
            [.. plan.Options.Select(option => ToDto(option, today, now))]);

    private WorkoutPlanOptionDto ToDto(WorkoutPlanOption option, DateOnly? today, DateTimeOffset? now) =>
        new(
            option.Id.Value,
            option.Position,
            [.. option.MuscleGroupIds.Select(id => new MuscleGroupDto(
                id.Value,
                muscleGroups.All.First(group => group.Id == id).Name.Value))],
            Today: today is null || now is null
                ? null
                : sessions.All
                    .Where(session =>
                        session.PlannedOptionId == option.Id && session.WorkoutDate.Value == today.Value)
                    .Select(session => new TodayWorkoutDto(
                        session.Id.Value,
                        session.StartedAt,
                        session.FinishedAt,
                        (int)session.Duration(now.Value).TotalSeconds,
                        session.IsRunning,
                        [.. session.PerformedSets.Select(set => new PerformedSetDto(
                            set.ExerciseId.Value,
                            set.Load.Kilograms,
                            set.Repetitions.Value))]))
                    .FirstOrDefault());
}
