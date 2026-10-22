using FitTrack.Domain.Common;
using FitTrack.Domain.Identity;

namespace FitTrack.Domain.WorkoutPlanning;

/// <summary>
/// Planning aggregate root: the reusable series a personal trainer authors, holding up to
/// <see cref="MaxWorkouts"/> specific workouts labelled A..E. The series is a template — assigning it
/// to athletes is a separate aggregate — so the same series serves many people without being copied.
/// </summary>
public sealed class WorkoutSeries
{
    public const int MaxWorkouts = 5;

    private readonly List<WorkoutSeriesWorkout> _workouts = [];

    private WorkoutSeries(WorkoutSeriesId id, WorkoutSeriesName name, UserId trainerId, UserId? athleteId)
    {
        Id = id;
        Name = name;
        TrainerId = trainerId;
        AthleteId = athleteId;
    }

    public WorkoutSeriesId Id { get; }

    public WorkoutSeriesName Name { get; private set; }

    /// <summary>
    /// Author of the series. Only this author may edit it. A personal trainer authors the series of
    /// their athletes; an athlete the administrator released to build their own plan authors it for
    /// themselves — then the author is the athlete, and the series belongs to them.
    /// </summary>
    public UserId TrainerId { get; }

    /// <summary>
    /// The single athlete the series was authored for: the series of a shared plan — authored for
    /// more than one athlete of the roster, each holding their own assignment — carries none, and so
    /// does a legacy series created before the per-athlete rule. Who actually follows the series
    /// lives on the assignments, not here.
    /// </summary>
    public UserId? AthleteId { get; }

    /// <summary>Workouts in <see cref="WorkoutSeriesWorkout.Position"/> order, from 1 to n.</summary>
    public IReadOnlyList<WorkoutSeriesWorkout> Workouts => _workouts;

    public static WorkoutSeries Create(WorkoutSeriesId id, WorkoutSeriesName name, UserId trainerId, UserId? athleteId = null)
    {
        if (trainerId.Value == Guid.Empty)
        {
            throw new DomainException("workout_series.trainer.required", "A workout series must have an author.");
        }

        if (athleteId is not null && athleteId.Value.Value == Guid.Empty)
        {
            throw new DomainException("workout_series.athlete.required", "A workout series must belong to an athlete.");
        }

        return new WorkoutSeries(id, name, trainerId, athleteId);
    }

    public void Rename(WorkoutSeriesName name) => Name = name;

    public WorkoutSeriesWorkout AddWorkout(
        WorkoutSeriesWorkoutId id,
        WorkoutName name,
        IReadOnlyList<PrescribedExercise> exercises)
    {
        if (_workouts.Count >= MaxWorkouts)
        {
            throw new DomainException(
                "workout_series.workout.limit_reached",
                $"A workout series holds at most {MaxWorkouts} workouts.");
        }

        var workout = new WorkoutSeriesWorkout(id, _workouts.Count + 1, name, exercises);
        _workouts.Add(workout);

        return workout;
    }

    public void ChangeWorkout(
        WorkoutSeriesWorkoutId id,
        WorkoutName name,
        IReadOnlyList<PrescribedExercise> exercises) =>
        FindWorkout(id).Change(name, exercises);

    public void RemoveWorkout(WorkoutSeriesWorkoutId id)
    {
        _workouts.Remove(FindWorkout(id));

        // Positions stay 1..n so the labels stay A..E with no hole on the athlete's sheet.
        for (var index = 0; index < _workouts.Count; index++)
        {
            _workouts[index].MoveTo(index + 1);
        }
    }

    public WorkoutSeriesWorkout FindWorkout(WorkoutSeriesWorkoutId id) =>
        _workouts.FirstOrDefault(workout => workout.Id == id)
            ?? throw new DomainException(
                "workout_series.workout.not_found",
                $"Workout '{id.Value}' is not part of this series.");
}
