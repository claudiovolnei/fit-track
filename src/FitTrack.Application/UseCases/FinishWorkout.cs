using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;
using FitTrack.Application.Exceptions;
using FitTrack.Application.Mapping;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Common;
using FitTrack.Domain.WorkoutCatalog;
using FitTrack.Domain.WorkoutExecution;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Application.UseCases;

/// <summary>
/// Stops the timer and closes the workout. The instant comes from the clock port, and finishing twice
/// is refused with the aggregate's own code.
/// <para>
/// The screen keeps the marks of the exercises on its own and sends everything when the athlete
/// finishes, so the workout arrives here in one piece: the exercises they marked, the effort of each
/// one, the level they gave the session and what they wrote about it. Two rules are checked here
/// because the prescription lives in the series context while the session only keeps the chosen
/// workout by id, with no foreign key: the workout only closes with every exercise of the
/// prescription marked, and nothing outside the prescription may come in.
/// </para>
/// </summary>
public sealed class FinishWorkout : IFinishWorkout
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkoutSeriesRepository _series;
    private readonly IWorkoutSessionRepository _sessions;
    private readonly IClock _clock;

    public FinishWorkout(
        ICurrentUser currentUser,
        IWorkoutSeriesRepository series,
        IWorkoutSessionRepository sessions,
        IClock clock)
    {
        _currentUser = currentUser;
        _series = series;
        _sessions = sessions;
        _clock = clock;
    }

    public async Task<WorkoutSessionDto> ExecuteAsync(
        FinishWorkoutCommand command,
        CancellationToken cancellationToken)
    {
        var session = await WorkoutSessionAccess.RequireOwnedAsync(
            _sessions,
            command.WorkoutSessionId,
            _currentUser.Id,
            cancellationToken);

        var intensity = command.Intensity
            ?? throw new DomainException(
                "session.finish.intensity.invalid",
                "Choose the intensity of the workout.");

        WorkoutNotes? notes = string.IsNullOrWhiteSpace(command.Notes)
            ? null
            : new WorkoutNotes(command.Notes);

        var prescription = await ReadPrescriptionAsync(session, cancellationToken);

        var performed = BuildPerformedExercises(command.Exercises, prescription);

        var now = _clock.UtcNow;

        session.Complete(performed, intensity, notes, now);

        await _sessions.SaveAsync(session, cancellationToken);

        return session.ToDto(now);
    }

    /// <summary>
    /// The prescription of the workout the session came from. A workout whose prescription is gone —
    /// the trainer removed it while the athlete was training — closes without the rules: there is
    /// nothing left to mark.
    /// </summary>
    private async Task<IReadOnlyList<PrescribedExercise>> ReadPrescriptionAsync(
        WorkoutSession session,
        CancellationToken cancellationToken)
    {
        if (session.PlannedWorkoutId is not { } workoutId)
        {
            return [];
        }

        var workout = await _series.GetWorkoutAsync(workoutId, cancellationToken);

        return workout?.Exercises ?? [];
    }

    /// <summary>
    /// What the session records, out of what the athlete sent: one entry per prescribed exercise,
    /// with the sets of the prescription and the repetitions and load the athlete really used. The
    /// two rules of the boundary are checked here — nothing outside the prescription comes in, and
    /// every exercise of the prescription is there.
    /// </summary>
    private static IReadOnlyList<PerformedExercise> BuildPerformedExercises(
        IReadOnlyList<PerformedExerciseInput> sent,
        IReadOnlyList<PrescribedExercise> prescription)
    {
        // The effort the athlete reports has to be an effort the app understands, whichever path it
        // comes by.
        foreach (var entry in sent)
        {
            if (entry.Repetitions is < 1 or > 100)
            {
                throw new DomainException(
                    "set.repetitions.out_of_range",
                    "The repetitions of a set have to be between 1 and 100.");
            }
        }

        // A workout whose prescription is gone has nothing to compare with: what the athlete sent
        // is what the session records.
        if (prescription.Count == 0)
        {
            return
            [
                .. sent.Select(entry => new PerformedExercise(
                    entry.ToExerciseId(),
                    new Load(entry.LoadKilograms),
                    new Repetitions(entry.Repetitions),
                    PerformedExercise.MinSets)),
            ];
        }

        var byExercise = prescription.ToDictionary(
            prescribed => prescribed.ExerciseId,
            prescribed => prescribed);

        foreach (var entry in sent)
        {
            var exerciseId = entry.ToExerciseId();

            if (!byExercise.ContainsKey(exerciseId))
            {
                throw new DomainException(
                    "session.set.exercise.not_prescribed",
                    "The exercise is not part of the prescribed workout.");
            }
        }

        var performed = new List<PerformedExercise>(prescription.Count);

        foreach (var prescribed in prescription)
        {
            var entry = sent.FirstOrDefault(candidate => candidate.ToExerciseId() == prescribed.ExerciseId);

            if (entry is null)
            {
                throw new ConflictException(
                    "session.finish.exercises.pending",
                    "Every exercise of the workout has to be ticked before it closes.");
            }

            performed.Add(new PerformedExercise(
                prescribed.ExerciseId,
                new Load(entry.LoadKilograms),
                new Repetitions(entry.Repetitions),
                prescribed.Sets));
        }

        return performed;
    }
}
