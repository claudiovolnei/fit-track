using FitTrack.Application.Dtos;
using FitTrack.Application.Exceptions;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Application.UseCases;

/// <summary>
/// The access rules of the planning use cases. A trainer only reaches the series they authored: an
/// unknown series is answered with <see cref="NotFoundException"/> and a series authored by a
/// different author with <see cref="ForbiddenException"/>, so a mutation states "not yours" while
/// <see cref="GetWorkoutSeries"/>, which has no aggregate to protect, answers 404 for both.
/// <para>
/// The author of a series is a personal trainer, or the athlete themselves when the administrator
/// released them to build their own plan. In both cases the series carries the author in
/// <see cref="WorkoutSeries.TrainerId"/>, so the ownership rule below holds for either.
/// </para>
/// </summary>
internal static class WorkoutSeriesAccess
{
    /// <summary>
    /// Who may author a training plan: a personal trainer, or an athlete the administrator released
    /// to build their own. The release lives in the account, not in the token, so it is read from
    /// the database and a revocation takes effect at once.
    /// </summary>
    public static async Task RequireAuthorAsync(
        ICurrentUser currentUser,
        IAccountPermissions permissions,
        string action,
        CancellationToken cancellationToken)
    {
        if (currentUser.IsPersonalTrainer)
        {
            return;
        }

        if (currentUser.IsAthlete
            && await permissions.CanAuthorOwnWorkoutAsync(currentUser.Id, cancellationToken))
        {
            return;
        }

        throw new ForbiddenException(
            $"Only personal trainers, or athletes released by the administrator, can {action}.");
    }

    public static async Task<WorkoutSeries> RequireOwnedAsync(
        IWorkoutSeriesRepository series,
        Guid seriesId,
        UserId trainerId,
        CancellationToken cancellationToken)
    {
        var found = await series.GetByIdAsync(new WorkoutSeriesId(seriesId), cancellationToken)
            ?? throw new NotFoundException($"Workout series '{seriesId}' was not found.");

        if (found.TrainerId != trainerId)
        {
            throw new ForbiddenException("Only the author of a workout series can manage it.");
        }

        return found;
    }

    /// <summary>
    /// Reads a series back through the query port after a write, so the answer is the same projection
    /// the read side serves instead of a second mapping that could drift.
    /// </summary>
    public static async Task<WorkoutSeriesDto> RequireDtoAsync(
        IQueryWorkoutSeries query,
        WorkoutSeriesId seriesId,
        CancellationToken cancellationToken) =>
        await query.GetByIdAsync(seriesId, cancellationToken)
            ?? throw new NotFoundException($"Workout series '{seriesId.Value}' was not found.");
}
