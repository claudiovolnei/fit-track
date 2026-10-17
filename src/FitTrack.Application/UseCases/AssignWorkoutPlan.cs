using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;
using FitTrack.Application.Exceptions;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Application.UseCases;

/// <summary>
/// Assigns the plan of an athlete. One plan per athlete: when one already exists it is returned as is
/// and the call reports that nothing was created. Restricted to personal trainers.
/// </summary>
public sealed class AssignWorkoutPlan : IAssignWorkoutPlan
{
    private readonly ICurrentUser _currentUser;
    private readonly IQueryAthletes _athletes;
    private readonly IWorkoutPlanRepository _plans;
    private readonly IQueryWorkoutPlans _query;
    private readonly IIdGenerator _idGenerator;

    public AssignWorkoutPlan(
        ICurrentUser currentUser,
        IQueryAthletes athletes,
        IWorkoutPlanRepository plans,
        IQueryWorkoutPlans query,
        IIdGenerator idGenerator)
    {
        _currentUser = currentUser;
        _athletes = athletes;
        _plans = plans;
        _query = query;
        _idGenerator = idGenerator;
    }

    public async Task<AssignWorkoutPlanResult> ExecuteAsync(
        AssignWorkoutPlanCommand command,
        CancellationToken cancellationToken)
    {
        _currentUser.RequirePersonalTrainer("assign a workout plan");

        var athleteId = new UserId(command.UserId);

        if (!await _athletes.ExistsAsync(athleteId, cancellationToken))
        {
            throw new NotFoundException($"Athlete '{command.UserId}' was not found.");
        }

        var existing = await _plans.GetByUserIdAsync(athleteId, cancellationToken);

        if (existing is not null)
        {
            if (existing.TrainerId != _currentUser.Id)
            {
                throw new ForbiddenException("This athlete already has a plan authored by another personal trainer.");
            }

            return new AssignWorkoutPlanResult(
                await RequirePlanAsync(existing.Id, cancellationToken),
                Created: false);
        }

        var plan = WorkoutPlan.Start(new WorkoutPlanId(_idGenerator.NewId()), athleteId, _currentUser.Id);

        await _plans.SaveAsync(plan, cancellationToken);

        return new AssignWorkoutPlanResult(await RequirePlanAsync(plan.Id, cancellationToken), Created: true);
    }

    private async Task<WorkoutPlanDto> RequirePlanAsync(
        WorkoutPlanId planId,
        CancellationToken cancellationToken) =>
        await _query.GetByIdAsync(planId, cancellationToken)
            ?? throw new NotFoundException($"Workout plan '{planId.Value}' was not found.");
}
