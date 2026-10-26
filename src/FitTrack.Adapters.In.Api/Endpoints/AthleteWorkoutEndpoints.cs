using FitTrack.Adapters.In.Api.Authentication;
using FitTrack.Adapters.In.Api.Composition;
using FitTrack.Application.Commands;
using FitTrack.Application.Ports.In;

namespace FitTrack.Adapters.In.Api.Endpoints;

internal static class AthleteWorkoutEndpoints
{
    public static IEndpointRouteBuilder MapAthleteWorkoutEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/athletes")
            .WithTags("Athletes")
            .RequireAuthorization(Policies.TrainerOrAdmin);

        group.MapDelete("/{userId:guid}/workouts/{sessionId:guid}", DeleteAthleteWorkoutAsync)
            .RequireAuthorization(Policies.AdminOnly);

        return app;
    }

    private static async Task<IResult> DeleteAthleteWorkoutAsync(
        Guid userId,
        Guid sessionId,
        IDeleteAthleteWorkout useCase,
        CancellationToken cancellationToken)
    {
        await useCase.ExecuteAsync(new DeleteAthleteWorkoutCommand(userId, sessionId), cancellationToken);

        return Results.NoContent();
    }
}
