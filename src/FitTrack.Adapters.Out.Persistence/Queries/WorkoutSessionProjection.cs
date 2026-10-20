using FitTrack.Adapters.Out.Persistence.Records;
using FitTrack.Application.Dtos;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Adapters.Out.Persistence.Queries;

/// <summary>
/// One session as the database returns it: the stored clock plus the sets, still without the elapsed
/// seconds. The duration is derived after materialization because the subtraction and the unit
/// conversion are not translatable on every provider — a rule worth keeping in one place.
/// </summary>
internal sealed record WorkoutSessionRow(
    Guid Id,
    DateOnly WorkoutDate,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    Guid? WorkoutSeriesWorkoutId,
    IReadOnlyList<PerformedSetDto> Sets)
{
    /// <summary>Elapsed seconds at <paramref name="now"/>: frozen once finished, running until then.</summary>
    public WorkoutSessionDto ToDto(DateTimeOffset now, WorkoutVolumeDto? volume = null) =>
        new(
            Id,
            WorkoutDate,
            StartedAt,
            FinishedAt,
            (int)((FinishedAt ?? now) - StartedAt).TotalSeconds,
            WorkoutSeriesWorkoutId,
            Sets,
            volume);
}

/// <summary>
/// Shared projection from session records to the read model. Kept in one place so the query port and
/// its translation test can never drift apart.
/// </summary>
internal static class WorkoutSessionProjection
{
    public static IQueryable<WorkoutSessionRow> ToWorkoutSessionRows(
        this IQueryable<WorkoutSessionRecord> sessions) =>
        sessions
            .OrderByDescending(session => session.WorkoutDate)
            .ThenByDescending(session => session.StartedAt)
            .Select(session => new WorkoutSessionRow(
                session.Id,
                session.WorkoutDate,
                session.StartedAt,
                session.FinishedAt,
                session.WorkoutSeriesWorkoutId,
                session.Sets
                    .OrderBy(set => set.Position)
                    .Select(set => new PerformedSetDto(
                        set.ExerciseId,
                        set.LoadKilograms,
                        set.Repetitions))
                    .ToList()));

    public static async Task<IReadOnlyList<WorkoutSessionDto>> ListDtosAsync(
        this IQueryable<WorkoutSessionRecord> sessions,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var rows = await sessions.ToWorkoutSessionRows().ToListAsync(cancellationToken);

        return [.. rows.Select(row => row.ToDto(now))];
    }
}
