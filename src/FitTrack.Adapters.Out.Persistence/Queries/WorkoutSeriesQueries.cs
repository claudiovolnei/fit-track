using FitTrack.Adapters.Out.Persistence.Records;
using FitTrack.Application.Dtos;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutPlanning;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Adapters.Out.Persistence.Queries;

/// <summary>
/// Read-side adapter for the series screens. It loads the pieces with a handful of set-based queries and
/// composes in memory, so the shape of the read model never turns into an N+1 over the workouts — and
/// the exercise names come from the catalog in one join instead of a lookup per row.
/// </summary>
internal sealed class WorkoutSeriesQueries(FitTrackDbContext dbContext) : IQueryWorkoutSeries
{
    public async Task<IReadOnlyList<WorkoutSeriesDto>> ListByTrainerAsync(
        UserId trainerId,
        CancellationToken cancellationToken)
    {
        var series = await dbContext.WorkoutSeries
            .AsNoTracking()
            .Where(candidate => candidate.TrainerId == trainerId.Value)
            .OrderBy(candidate => candidate.Name)
            .Select(candidate => new SeriesRow(candidate.Id, candidate.Name, candidate.TrainerId, candidate.AthleteId))
            .ToListAsync(cancellationToken);

        return await ComposeAsync(series, athlete: null, today: null, now: null, cancellationToken);
    }

    public async Task<WorkoutSeriesDto?> GetByIdAsync(WorkoutSeriesId seriesId, CancellationToken cancellationToken)
    {
        var series = await dbContext.WorkoutSeries
            .AsNoTracking()
            .Where(candidate => candidate.Id == seriesId.Value)
            .Select(candidate => new SeriesRow(candidate.Id, candidate.Name, candidate.TrainerId, candidate.AthleteId))
            .FirstOrDefaultAsync(cancellationToken);

        if (series is null)
        {
            return null;
        }

        var composed = await ComposeAsync([series], athlete: null, today: null, now: null, cancellationToken);

        return composed[0];
    }

    public async Task<MyWorkoutSeriesDto?> GetForAthleteAsync(
        UserId userId,
        DateOnly today,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var assignment = await dbContext.WorkoutSeriesAssignments
            .AsNoTracking()
            .Where(candidate => candidate.UserId == userId.Value)
            .Select(candidate => new { candidate.WorkoutSeriesId, candidate.ExpiresAt })
            .FirstOrDefaultAsync(cancellationToken);

        if (assignment is null)
        {
            return null;
        }

        var series = await dbContext.WorkoutSeries
            .AsNoTracking()
            .Where(candidate => candidate.Id == assignment.WorkoutSeriesId)
            .Select(candidate => new SeriesRow(candidate.Id, candidate.Name, candidate.TrainerId, candidate.AthleteId))
            .FirstOrDefaultAsync(cancellationToken);

        if (series is null)
        {
            return null;
        }

        var trainer = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == series.TrainerId)
            .Select(user => new { user.Name, user.Email })
            .FirstOrDefaultAsync(cancellationToken);

        var composed = await ComposeAsync([series], userId, today, now, cancellationToken);

        return new MyWorkoutSeriesDto(
            composed[0],
            TrainerDisplayName(trainer?.Name, trainer?.Email),
            assignment.ExpiresAt,
            WorkoutSeriesAssignment.DaysRemainingUntil(assignment.ExpiresAt, now),
            WorkoutSeriesAssignment.IsExpired(assignment.ExpiresAt, now));
    }

    /// <summary>
    /// Who the athlete trains with. The e-mail stands in while the account has no display name, so the
    /// screen never shows an empty personal.
    /// </summary>
    private static string TrainerDisplayName(string? name, string? email) =>
        !string.IsNullOrWhiteSpace(name)
            ? name
            : string.IsNullOrWhiteSpace(email) ? "Personal trainer" : email;

    private async Task<IReadOnlyList<WorkoutSeriesDto>> ComposeAsync(
        IReadOnlyList<SeriesRow> series,
        UserId? athlete,
        DateOnly? today,
        DateTimeOffset? now,
        CancellationToken cancellationToken)
    {
        var seriesIds = series.Select(candidate => candidate.Id).ToList();

        var workouts = await dbContext.WorkoutSeriesWorkouts
            .AsNoTracking()
            .Where(workout => seriesIds.Contains(workout.WorkoutSeriesId))
            .OrderBy(workout => workout.Position)
            .Select(workout => new { workout.Id, workout.WorkoutSeriesId, workout.Position, workout.Name })
            .ToListAsync(cancellationToken);

        var workoutIds = workouts.Select(workout => workout.Id).ToList();

        var exercises = await dbContext.WorkoutSeriesWorkoutExercises
            .AsNoTracking()
            .Where(exercise => workoutIds.Contains(exercise.WorkoutSeriesWorkoutId))
            .OrderBy(exercise => exercise.Position)
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
                    ExerciseId = row.catalogExercise.Id,
                    ExerciseName = row.catalogExercise.Name,
                    MuscleGroupName = muscleGroup.Name,
                    row.exercise.Sets,
                    row.exercise.Repetitions,
                    row.exercise.LoadKilograms,
                })
            .ToListAsync(cancellationToken);

        // The workouts already started today, per workout. Only the athlete's own view needs them, and
        // only the athlete's own sessions count: the same workout can be running for two athletes.
        var todayByWorkout = athlete is null || today is null || now is null
            ? new Dictionary<Guid, TodayWorkoutDto>()
            : await LoadTodayAsync(athlete.Value, today.Value, now.Value, workoutIds, cancellationToken);

        var workoutsBySeries = workouts.ToLookup(workout => workout.WorkoutSeriesId);
        var exercisesByWorkout = exercises.ToLookup(exercise => exercise.WorkoutSeriesWorkoutId);

        var result = new List<WorkoutSeriesDto>(series.Count);

        foreach (var candidate in series)
        {
            var workoutDtos = new List<WorkoutSeriesWorkoutDto>();

            foreach (var workout in workoutsBySeries[candidate.Id].OrderBy(workout => workout.Position))
            {
                var exerciseDtos = exercisesByWorkout[workout.Id]
                    .OrderBy(exercise => exercise.Position)
                    .Select(exercise => new PrescribedExerciseDto(
                        exercise.ExerciseId,
                        exercise.ExerciseName,
                        exercise.MuscleGroupName,
                        exercise.Sets,
                        exercise.Repetitions,
                        exercise.LoadKilograms))
                    .ToList();

                workoutDtos.Add(new WorkoutSeriesWorkoutDto(
                    workout.Id,
                    workout.Position,
                    WorkoutSeriesWorkout.LabelFor(workout.Position),
                    workout.Name,
                    exerciseDtos,
                    todayByWorkout.GetValueOrDefault(workout.Id)));
            }

            result.Add(new WorkoutSeriesDto(candidate.Id, candidate.Name, candidate.TrainerId, candidate.AthleteId, workoutDtos));
        }

        return result;
    }

    /// <summary>
    /// The sessions already started today, by workout. The elapsed seconds are derived after
    /// materialization because the subtraction and the unit conversion are not translatable on every
    /// provider — the same rule the session projection already follows.
    /// </summary>
    private async Task<Dictionary<Guid, TodayWorkoutDto>> LoadTodayAsync(
        UserId userId,
        DateOnly today,
        DateTimeOffset now,
        IReadOnlyList<Guid> workoutIds,
        CancellationToken cancellationToken)
    {
        var sessions = await dbContext.WorkoutSessions
            .AsNoTracking()
            .Include(session => session.Sets)
            .Where(session =>
                session.UserId == userId.Value
                && session.WorkoutDate == today
                && session.WorkoutSeriesWorkoutId != null
                && workoutIds.Contains(session.WorkoutSeriesWorkoutId.Value))
            .ToListAsync(cancellationToken);

        return sessions.ToDictionary(
            session => session.WorkoutSeriesWorkoutId!.Value,
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

    private sealed record SeriesRow(Guid Id, string Name, Guid TrainerId, Guid? AthleteId);
}
