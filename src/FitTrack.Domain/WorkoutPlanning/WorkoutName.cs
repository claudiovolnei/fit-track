using FitTrack.Domain.Common;

namespace FitTrack.Domain.WorkoutPlanning;

/// <summary>Value object holding the name of one specific workout of a series.</summary>
public readonly record struct WorkoutName
{
    public const int MinLength = 2;

    public const int MaxLength = 80;

    public WorkoutName(string value)
    {
        var trimmed = value?.Trim() ?? string.Empty;

        if (trimmed.Length < MinLength)
        {
            throw new DomainException(
                "workout_series.workout.name.required",
                $"Workout name must have at least {MinLength} characters.");
        }

        if (trimmed.Length > MaxLength)
        {
            throw new DomainException(
                "workout_series.workout.name.too_long",
                $"Workout name must be at most {MaxLength} characters.");
        }

        Value = trimmed;
    }

    public string Value { get; }

    public override string ToString() => Value;
}
