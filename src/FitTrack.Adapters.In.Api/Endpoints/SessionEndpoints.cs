using FitTrack.Adapters.In.Api.Contracts;
using FitTrack.Application.Commands;
using FitTrack.Application.Ports.In;

namespace FitTrack.Adapters.In.Api.Endpoints;

/// <summary>
/// Inbound adapter for the athlete's workout. The POST that used to record a session on any date now
/// means "start today's workout from the workout I picked": the clock is the server's, the session is
/// unique per workout per day, and the timer lives until the athlete finishes.
/// </summary>
internal static class SessionEndpoints
{
    public static IEndpointRouteBuilder MapSessionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/sessions")
            .WithTags("Workout sessions")
            .RequireAuthorization();

        group.MapGet("/", ListAsync);
        group.MapPost("/", StartAsync);
        group.MapPost("/{sessionId:guid}/sets", LogSetAsync);
        group.MapPost("/{sessionId:guid}/finish", FinishAsync);
        group.MapDelete("/{sessionId:guid}", CancelAsync);

        return app;
    }

    private static async Task<IResult> ListAsync(IListUserSessions useCase, CancellationToken cancellationToken) =>
        Results.Ok(await useCase.ExecuteAsync(cancellationToken));

    private static async Task<IResult> StartAsync(
        StartWorkoutRequest request,
        IStartWorkout useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(
            new StartWorkoutCommand(request.WorkoutSeriesWorkoutId),
            cancellationToken);

        return result.Started
            ? Results.Created($"/sessions/{result.Session.Id}", result.Session)
            : Results.Ok(result.Session);
    }

    private static async Task<IResult> LogSetAsync(
        Guid sessionId,
        LogPerformedSetRequest request,
        ILogPerformedSet useCase,
        CancellationToken cancellationToken)
    {
        var session = await useCase.ExecuteAsync(
            new LogPerformedSetCommand(sessionId, request.ExerciseId, request.LoadKilograms),
            cancellationToken);

        return Results.Created($"/sessions/{sessionId}", session);
    }

    private static async Task<IResult> FinishAsync(
        Guid sessionId,
        IFinishWorkout useCase,
        CancellationToken cancellationToken) =>
        Results.Ok(await useCase.ExecuteAsync(new FinishWorkoutCommand(sessionId), cancellationToken));

    private static async Task<IResult> CancelAsync(
        Guid sessionId,
        ICancelWorkout useCase,
        CancellationToken cancellationToken)
    {
        await useCase.ExecuteAsync(new CancelWorkoutCommand(sessionId), cancellationToken);

        return Results.NoContent();
    }
}
