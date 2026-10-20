using FitTrack.Adapters.Out.Persistence.Records;
using FitTrack.Application.Dtos;
using FitTrack.Application.Ports.Out;
using FitTrack.Application.UseCases;
using FitTrack.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Adapters.Out.Persistence.Queries;

/// <summary>
/// Read-side adapter for the workout history. The volume comparison is resolved in one batch over the
/// workouts of the page (and of the whole filtered history for the summary), so the screen never pays
/// a query per session. A session whose workout no longer exists stays readable and simply has no
/// comparison.
/// </summary>
internal sealed class UserSessionQueries(FitTrackDbContext dbContext, IClock clock) : IQueryUserSessions
{
    public async Task<IReadOnlyList<WorkoutSessionDto>> ListByUserAsync(
        UserId userId,
        CancellationToken cancellationToken) =>
        (await ListByUserAsync(userId, ListQuery.All, cancellationToken)).Items;

    public async Task<WorkoutHistoryDto> ListByUserAsync(
        UserId userId,
        ListQuery query,
        CancellationToken cancellationToken)
    {
        var sessions = dbContext.WorkoutSessions
            .AsNoTracking()
            .Where(session => session.UserId == userId.Value);

        if (query.Search is { } search)
        {
            var pattern = SearchPattern.Contains(search);

            sessions = sessions.Where(session =>
                session.WorkoutSeriesWorkoutId != null
                && dbContext.WorkoutSeriesWorkouts.Any(workout =>
                    workout.Id == session.WorkoutSeriesWorkoutId.Value
                    && EF.Functions.ILike(workout.Name, pattern, "\\")));
        }

        var total = await sessions.CountAsync(cancellationToken);

        var ordered = sessions
            .OrderByDescending(session => session.WorkoutDate)
            .ThenByDescending(session => session.StartedAt)
            .ThenByDescending(session => session.Id);

        var page = query.IsPaged
            ? ordered.Skip(query.Skip).Take(query.Take)
            : ordered;

        var rows = await page.ToWorkoutSessionRows().ToListAsync(cancellationToken);
        var items = await ComposeAsync(rows, cancellationToken);

        // The statistics cards describe every filtered session, not only the page in front of the
        // athlete. The unpaged path already loaded them, so only the paged path repeats the read.
        var summarySessions = query.IsPaged
            ? await ComposeAsync(await sessions.ToWorkoutSessionRows().ToListAsync(cancellationToken), cancellationToken)
            : items;

        return new WorkoutHistoryDto(
            items,
            query.Page,
            query.PageSize,
            total,
            WorkoutVolumeCalculator.Summarize(summarySessions));
    }

    private async Task<IReadOnlyList<WorkoutSessionDto>> ComposeAsync(
        IReadOnlyList<WorkoutSessionRow> rows,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var exerciseNames = await LoadExerciseNamesAsync(rows, cancellationToken);

        var workoutIds = rows
            .Where(row => row.WorkoutSeriesWorkoutId is not null)
            .Select(row => row.WorkoutSeriesWorkoutId!.Value)
            .Distinct()
            .ToList();

        var prescriptions = await LoadPrescriptionsAsync(workoutIds, cancellationToken);
        var existingWorkouts = await LoadExistingWorkoutIdsAsync(workoutIds, cancellationToken);

        return
        [
            .. rows.Select(row =>
            {
                // A null reference and an orphan reference are both "no prescription to compare".
                var prescription = row.WorkoutSeriesWorkoutId is { } workoutId
                    && existingWorkouts.Contains(workoutId)
                        ? prescriptions.GetValueOrDefault(workoutId)
                        : null;

                var volume = prescription is null
                    ? null
                    : WorkoutVolumeCalculator.Calculate(row.Sets, prescription, exerciseNames);

                return row.ToDto(now, volume);
            }),
        ];
    }

    private async Task<Dictionary<Guid, string>> LoadExerciseNamesAsync(
        IReadOnlyList<WorkoutSessionRow> rows,
        CancellationToken cancellationToken)
    {
        var exerciseIds = rows
            .SelectMany(row => row.Sets)
            .Select(set => set.ExerciseId)
            .Distinct()
            .ToList();

        if (exerciseIds.Count == 0)
        {
            return [];
        }

        return await dbContext.Exercises
            .AsNoTracking()
            .Where(exercise => exerciseIds.Contains(exercise.Id))
            .Select(exercise => new { exercise.Id, exercise.Name })
            .ToDictionaryAsync(exercise => exercise.Id, exercise => exercise.Name, cancellationToken);
    }

    private async Task<Dictionary<Guid, IReadOnlyList<PrescribedExerciseDto>>> LoadPrescriptionsAsync(
        IReadOnlyList<Guid> workoutIds,
        CancellationToken cancellationToken)
    {
        if (workoutIds.Count == 0)
        {
            return [];
        }

        var rows = await dbContext.WorkoutSeriesWorkoutExercises
            .AsNoTracking()
            .Where(exercise => workoutIds.Contains(exercise.WorkoutSeriesWorkoutId))
            .Join(
                dbContext.Exercises.AsNoTracking(),
                exercise => exercise.ExerciseId,
                catalogExercise => catalogExercise.Id,
                (exercise, catalogExercise) => new { exercise, catalogExercise })
            .Join(
                dbContext.MuscleGroups.AsNoTracking(),
                row => row.catalogExercise.MuscleGroupId,
                muscleGroup => muscleGroup.Id,
                (row, muscleGroup) => new
                {
                    row.exercise.WorkoutSeriesWorkoutId,
                    row.exercise.Position,
                    row.exercise.ExerciseId,
                    ExerciseName = row.catalogExercise.Name,
                    MuscleGroupName = muscleGroup.Name,
                    row.exercise.Sets,
                    row.exercise.Repetitions,
                    row.exercise.LoadKilograms,
                })
            .OrderBy(row => row.WorkoutSeriesWorkoutId)
            .ThenBy(row => row.Position)
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => row.WorkoutSeriesWorkoutId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<PrescribedExerciseDto>)
                [
                    .. group.Select(row => new PrescribedExerciseDto(
                        row.ExerciseId,
                        row.ExerciseName,
                        row.MuscleGroupName,
                        row.Sets,
                        row.Repetitions,
                        row.LoadKilograms)),
                ]);
    }

    private async Task<HashSet<Guid>> LoadExistingWorkoutIdsAsync(
        IReadOnlyList<Guid> workoutIds,
        CancellationToken cancellationToken)
    {
        if (workoutIds.Count == 0)
        {
            return [];
        }

        var found = await dbContext.WorkoutSeriesWorkouts
            .AsNoTracking()
            .Where(workout => workoutIds.Contains(workout.Id))
            .Select(workout => workout.Id)
            .ToListAsync(cancellationToken);

        return [.. found];
    }
}
