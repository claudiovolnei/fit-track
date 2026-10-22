using FitTrack.Application.Dtos;
using FitTrack.Domain.WorkoutExecution;

namespace FitTrack.Application.Mapping;

/// <summary>
/// Translates the <see cref="WorkoutSession"/> aggregate into its application read model. The elapsed
/// seconds are computed at the instant the caller passes, never from a clock read inside the mapping.
/// </summary>
internal static class WorkoutSessionMapping
{
    public static WorkoutSessionDto ToDto(this WorkoutSession session, DateTimeOffset now) =>
        new(
            session.Id.Value,
            session.WorkoutDate.Value,
            session.StartedAt,
            session.FinishedAt,
            (int)session.Duration(now).TotalSeconds,
            session.PlannedWorkoutId?.Value,
            [.. session.PerformedSets.Select(set => new PerformedSetDto(
                set.ExerciseId.Value,
                set.Load.Kilograms,
                set.Repetitions.Value))])
        {
            CompletedExerciseIds = [.. session.CompletedExercises.Select(exercise => exercise.Value)],
            Intensity = WorkoutIntensityMapping.ToContract(session.Intensity),
            Notes = session.Notes?.Value,
        };
}
