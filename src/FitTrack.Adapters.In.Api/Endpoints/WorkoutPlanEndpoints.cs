using FitTrack.Adapters.In.Api.Authentication;
using FitTrack.Adapters.In.Api.Contracts;
using FitTrack.Application.Commands;
using FitTrack.Application.Ports.In;

namespace FitTrack.Adapters.In.Api.Endpoints;

/// <summary>
/// Inbound adapter for the planning slice. The trainer side carries the personal trainer policy; the
/// athlete side only requires an authenticated account, and each use case re-states who may act.
/// </summary>
internal static class WorkoutPlanEndpoints
{
    public static IEndpointRouteBuilder MapWorkoutPlanEndpoints(this IEndpointRouteBuilder app)
    {
        var athletes = app.MapGroup("/athletes")
            .WithTags("Athletes")
            .RequireAuthorization(Policies.PersonalTrainerOnly);

        athletes.MapGet("/", ListAthletesAsync);
        athletes.MapGet("/{userId:guid}/workouts", ListAthleteWorkoutsAsync);

        var plans = app.MapGroup("/workout-plans")
            .WithTags("Workout plans")
            .RequireAuthorization(Policies.PersonalTrainerOnly);

        plans.MapGet("/", ListPlansAsync);
        plans.MapGet("/{planId:guid}", GetPlanAsync);
        plans.MapPost("/", AssignAsync);
        plans.MapPost("/{planId:guid}/options", AddOptionAsync);
        plans.MapPut("/{planId:guid}/options/{optionId:guid}", ChangeOptionAsync);
        plans.MapDelete("/{planId:guid}/options/{optionId:guid}", RemoveOptionAsync);

        // The athlete reads their own plan through a singular route: no id, no way to ask for a plan
        // that is not theirs.
        app.MapGet("/workout-plan", GetMyPlanAsync)
            .WithTags("Workout plans")
            .RequireAuthorization();

        return app;
    }

    private static async Task<IResult> ListAthletesAsync(
        IListAthletes useCase,
        CancellationToken cancellationToken) =>
        Results.Ok(await useCase.ExecuteAsync(cancellationToken));

    private static async Task<IResult> ListAthleteWorkoutsAsync(
        Guid userId,
        IListAthleteWorkouts useCase,
        CancellationToken cancellationToken) =>
        Results.Ok(await useCase.ExecuteAsync(new ListAthleteWorkoutsCommand(userId), cancellationToken));

    private static async Task<IResult> ListPlansAsync(
        IListTrainerPlans useCase,
        CancellationToken cancellationToken) =>
        Results.Ok(await useCase.ExecuteAsync(cancellationToken));

    private static async Task<IResult> GetPlanAsync(
        Guid planId,
        IGetWorkoutPlan useCase,
        CancellationToken cancellationToken) =>
        Results.Ok(await useCase.ExecuteAsync(planId, cancellationToken));

    private static async Task<IResult> AssignAsync(
        AssignWorkoutPlanRequest request,
        IAssignWorkoutPlan useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(
            new AssignWorkoutPlanCommand(request.UserId),
            cancellationToken);

        return result.Created
            ? Results.Created($"/workout-plans/{result.Plan.Id}", result.Plan)
            : Results.Ok(result.Plan);
    }

    private static async Task<IResult> AddOptionAsync(
        Guid planId,
        SetWorkoutPlanOptionRequest request,
        IAddWorkoutPlanOption useCase,
        CancellationToken cancellationToken)
    {
        var option = await useCase.ExecuteAsync(
            new AddWorkoutPlanOptionCommand(planId, request.MuscleGroupIds),
            cancellationToken);

        return Results.Created($"/workout-plans/{planId}/options/{option.Id}", option);
    }

    private static async Task<IResult> ChangeOptionAsync(
        Guid planId,
        Guid optionId,
        SetWorkoutPlanOptionRequest request,
        IChangeWorkoutPlanOption useCase,
        CancellationToken cancellationToken) =>
        Results.Ok(await useCase.ExecuteAsync(
            new ChangeWorkoutPlanOptionCommand(planId, optionId, request.MuscleGroupIds),
            cancellationToken));

    private static async Task<IResult> RemoveOptionAsync(
        Guid planId,
        Guid optionId,
        IRemoveWorkoutPlanOption useCase,
        CancellationToken cancellationToken)
    {
        await useCase.ExecuteAsync(
            new RemoveWorkoutPlanOptionCommand(planId, optionId),
            cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult> GetMyPlanAsync(
        IGetMyWorkoutPlan useCase,
        CancellationToken cancellationToken) =>
        Results.Ok(await useCase.ExecuteAsync(cancellationToken));
}
