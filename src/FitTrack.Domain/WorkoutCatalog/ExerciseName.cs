using FitTrack.Domain.Common;

namespace FitTrack.Domain.WorkoutCatalog;

/// <summary>Value object holding the human-readable name of a catalog exercise.</summary>
public readonly record struct ExerciseName
{
    public const int MaxLength = 120;

    public ExerciseName(string value)
    {
        var trimmed = value?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            throw new DomainException("exercise.name.empty", "Exercise name is required.");
        }

        if (trimmed.Length > MaxLength)
        {
            throw new DomainException(
                "exercise.name.too_long",
                $"Exercise name must be at most {MaxLength} characters.");
        }

        Value = trimmed;
    }

    public string Value { get; }

    public override string ToString() => Value;
}
