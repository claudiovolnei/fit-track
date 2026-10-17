namespace FitTrack.Domain.WorkoutExecution;

/// <summary>Value object holding the calendar date a workout session happened on.</summary>
public readonly record struct WorkoutDate
{
    public WorkoutDate(DateOnly value) => Value = value;

    public DateOnly Value { get; }

    public override string ToString() => Value.ToString("yyyy-MM-dd");
}
