using FitTrack.Adapters.In.Api.Composition;
using FitTrack.Adapters.In.Api.Contracts;
using FitTrack.Application.Commands;
using FitTrack.Application.Ports.In;

namespace FitTrack.Adapters.In.Api.Endpoints;

/// <summary>
/// Inbound adapter for the athlete's workout. The POST that used to record a session on any date now
/// means "start today's workout from the workout I picked": the clock is the server's, the session is
/// unique per workout per day, and the timer lives until the athlete finishes. The tick of each
/// exercise is kept with the session, so the workout only closes with every exercise done.
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
        group.MapPut("/{sessionId:guid}/exercises/{exerciseId:guid}/completion", CompleteExerciseAsync);
        group.MapPost("/{sessionId:guid}/finish", FinishAsync);
        group.MapDelete("/{sessionId:guid}", CancelAsync);

        return app;
    }

    private static async Task<IResult> ListAsync(
        string? search,
        int? page,
        int? pageSize,
        IListUserSessions useCase,
        CancellationToken cancellationToken)
    {
        var invalid = ListRequest.TryRead(search, page, pageSize, out var query);
        if (invalid is not null)
        {
            return invalid;
        }

        var result = await useCase.ExecuteAsync(query, cancellationToken);

        return ListRequest.Ok(result, ListRequest.IsPaged(page, pageSize));
    }

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
            new LogPerformedSetCommand(sessionId, request.ExerciseId, request.LoadKilograms, request.Repetitions),
            cancellationToken);

        return Results.Created($"/sessions/{sessionId}", session);
    }

    private static async Task<IResult> CompleteExerciseAsync(
        Guid sessionId,
        Guid exerciseId,
        SetExerciseCompletionRequest request,
        IMarkExerciseCompleted useCase,
        CancellationToken cancellationToken)
    {
        var session = await useCase.ExecuteAsync(
            new MarkExerciseCompletedCommand(sessionId, exerciseId, request.Completed),
            cancellationToken);

        return Results.Ok(session);
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
