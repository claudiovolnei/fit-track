using FitTrack.Domain.Common;

namespace FitTrack.Domain.WorkoutPlanning;

/// <summary>Value object holding the name of a workout series.</summary>
public readonly record struct WorkoutSeriesName
{
    public const int MinLength = 2;

    public const int MaxLength = 80;

    public WorkoutSeriesName(string value)
    {
        var trimmed = value?.Trim() ?? string.Empty;

        if (trimmed.Length < MinLength)
        {
            throw new DomainException(
                "workout_series.name.required",
                $"Workout series name must have at least {MinLength} characters.");
        }

        if (trimmed.Length > MaxLength)
        {
            throw new DomainException(
                "workout_series.name.too_long",
                $"Workout series name must be at most {MaxLength} characters.");
        }

        Value = trimmed;
    }

    public string Value { get; }

    public override string ToString() => Value;
}
