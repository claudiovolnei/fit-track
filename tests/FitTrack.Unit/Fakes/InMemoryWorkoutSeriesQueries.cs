using FitTrack.Application.Dtos;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Unit.Fakes;

/// <summary>
/// Read-side fake for the series screens. It projects the series the write fakes hold, the same way the
/// SQL adapter projects the saved rows, so the unit test asserts the aggregate and not a copy of it.
/// </summary>
internal sealed class InMemoryWorkoutSeriesQueries(
    InMemoryWorkoutSeriesRepository series,
    InMemoryWorkoutSeriesAssignmentRepository assignments,
    InMemoryExerciseRepository exercises,
    InMemoryMuscleGroupRepository muscleGroups,
    InMemoryWorkoutSessionRepository sessions) : IQueryWorkoutSeries
{
    public Task<IReadOnlyList<WorkoutSeriesDto>> ListByTrainerAsync(
        UserId trainerId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<WorkoutSeriesDto>>(
            [.. series.All.Where(candidate => candidate.TrainerId == trainerId).Select(candidate => ToDto(candidate))]);

    public Task<PagedResult<WorkoutSeriesDto>> ListByTrainerAsync(
        UserId trainerId,
        ListQuery query,
        CancellationToken cancellationToken) =>
        Task.FromResult(FakePaging.Apply(
            series.All
                .Where(candidate => candidate.TrainerId == trainerId)
                .Select(candidate => ToDto(candidate))
                .OrderBy(candidate => candidate.Name),
            query,
            candidate => candidate.Name));

    public Task<WorkoutSeriesDto?> GetByIdAsync(WorkoutSeriesId seriesId, CancellationToken cancellationToken) =>
        Task.FromResult(series.All
            .Where(candidate => candidate.Id == seriesId)
            .Select(candidate => ToDto(candidate))
            .FirstOrDefault());

    public Task<MyWorkoutSeriesDto?> GetForAthleteAsync(
        UserId userId,
        DateOnly today,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var assignment = assignments.All.FirstOrDefault(candidate => candidate.UserId == userId);
        var assigned = assignment is null
            ? null
            : series.All.FirstOrDefault(candidate => candidate.Id == assignment.SeriesId);

        return Task.FromResult(assigned is null || assignment is null
            ? null
            : new MyWorkoutSeriesDto(
                ToDto(assigned, today, now),
                TrainerName,
                assignment.ExpiresAt,
                assignment.DaysRemaining(now),
                assignment.IsExpiredAt(now)));
    }

    /// <summary>Name the fake reports as the author of the series; the SQL adapter reads it off the account.</summary>
    public string TrainerName { get; set; } = "Personal trainer";

    private WorkoutSeriesDto ToDto(
        WorkoutSeries candidate,
        DateOnly? today = null,
        DateTimeOffset? now = null) =>
        new(
            candidate.Id.Value,
            candidate.Name.Value,
            candidate.TrainerId.Value,
            candidate.AthleteId?.Value,
            [.. candidate.Workouts.Select(workout => ToDto(workout, today, now))]);

    private WorkoutSeriesWorkoutDto ToDto(
        WorkoutSeriesWorkout workout,
        DateOnly? today,
        DateTimeOffset? now) =>
        new(
            workout.Id.Value,
            workout.Position,
            workout.Label,
            workout.Name.Value,
            [.. workout.Exercises.Select(ToDto)],
            Today(workout, today, now));

    private PrescribedExerciseDto ToDto(PrescribedExercise exercise)
    {
        var found = exercises.All.First(candidate => candidate.Id == exercise.ExerciseId);
        var muscleGroup = muscleGroups.All.First(group => group.Id == found.MuscleGroupId);

        return new PrescribedExerciseDto(
            exercise.ExerciseId.Value,
            found.Name.Value,
            muscleGroup.Name.Value,
            exercise.Sets,
            exercise.Repetitions,
            exercise.LoadKilograms,
            exercise.RestSeconds);
    }

    private TodayWorkoutDto? Today(WorkoutSeriesWorkout workout, DateOnly? today, DateTimeOffset? now) =>
        today is null || now is null
            ? null
            : sessions.All
                .Where(session =>
                    session.PlannedWorkoutId == workout.Id && session.WorkoutDate.Value == today.Value)
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
                .FirstOrDefault();
}

