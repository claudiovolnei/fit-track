namespace FitTrack.TestCases;

/// <summary>
/// One refusal the workout-series slice has to give, whichever side of the hexagon answers.
/// <see cref="Id"/> is the scenario the two suites drive; <see cref="Code"/> is the stable code that
/// scenario has to come back with.
/// </summary>
public sealed record SeriesRefusal(string Id, string Code);

/// <summary>
/// The refusals both test suites drive, and the answer each one owes.
/// <para>
/// The unit suite runs them through the use cases over the in-memory fakes; the integration suite runs
/// the same ones over HTTP against PostgreSQL. Both read this table, which is what makes the fake and
/// the adapter comparable: the day one of them stops refusing the same input the same way, the suite on
/// that side goes red instead of the two drifting apart behind a green build.
/// </para>
/// </summary>
public static class SeriesRefusals
{
    public const string SeriesNameRequired = "series-name-required";

    public const string SeriesNameDuplicate = "series-name-duplicate";

    public const string WorkoutNameRequired = "workout-name-required";

    public const string WorkoutPrescriptionEmpty = "workout-prescription-empty";

    public const string WorkoutPrescriptionDuplicateExercise = "workout-prescription-duplicate-exercise";

    public const string WorkoutPrescriptionOutOfRange = "workout-prescription-out-of-range";

    public const string WorkoutRestOutOfRange = "workout-rest-out-of-range";

    public const string WorkoutLimitReached = "workout-limit-reached";

    public const string WorkoutRemovedTwice = "workout-removed-twice";

    public const string AssignmentExpired = "assignment-expired";

    public const string SetOutsidePrescription = "set-outside-prescription";

    /// <summary>Every refusal with the code the edge answers with, in the order the suites drive them.</summary>
    public static IReadOnlyList<SeriesRefusal> All { get; } =
    [
        new(SeriesNameRequired, "workout_series.name.required"),
        new(SeriesNameDuplicate, "workout_series.name.duplicate"),
        new(WorkoutNameRequired, "workout_series.workout.name.required"),
        new(WorkoutPrescriptionEmpty, "workout_series.workout.exercises.empty"),
        new(WorkoutPrescriptionDuplicateExercise, "workout_series.exercise.duplicate"),
        new(WorkoutPrescriptionOutOfRange, "workout_series.exercise.prescription.invalid"),
        new(WorkoutRestOutOfRange, "workout_series.exercise.prescription.invalid"),
        new(WorkoutLimitReached, "workout_series.workout.limit_reached"),
        new(WorkoutRemovedTwice, "workout_series.workout.not_found"),
        new(AssignmentExpired, "workout_series.assignment.expired"),
        new(SetOutsidePrescription, "session.set.exercise.not_prescribed"),
    ];

    /// <summary>xUnit data: one row per refusal, so a failure names the rule that drifted.</summary>
    public static TheoryData<string, string> Refusals
    {
        get
        {
            var data = new TheoryData<string, string>();

            foreach (var refusal in All)
            {
                data.Add(refusal.Id, refusal.Code);
            }

            return data;
        }
    }
}
