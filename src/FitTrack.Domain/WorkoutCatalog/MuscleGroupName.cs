using FitTrack.Domain.Common;

namespace FitTrack.Domain.WorkoutCatalog;

/// <summary>Value object holding the name of a catalog muscle group.</summary>
public readonly record struct MuscleGroupName
{
    public const int MinLength = 2;

    public const int MaxLength = 60;

    public MuscleGroupName(string value)
    {
        var trimmed = value?.Trim() ?? string.Empty;

        if (trimmed.Length < MinLength)
        {
            throw new DomainException(
                "muscle_group.name.required",
                $"Muscle group name must have at least {MinLength} characters.");
        }

        if (trimmed.Length > MaxLength)
        {
            throw new DomainException(
                "muscle_group.name.too_long",
                $"Muscle group name must be at most {MaxLength} characters.");
        }

        Value = trimmed;
    }

    public string Value { get; }

    public override string ToString() => Value;
}
