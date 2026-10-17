using FitTrack.Domain.Common;

namespace FitTrack.Domain.WorkoutExecution;

/// <summary>Value object for how many times a performed set was repeated.</summary>
public readonly record struct Repetitions
{
    public Repetitions(int value)
    {
        if (value <= 0)
        {
            throw new DomainException(
                "set.repetitions.not_positive",
                "Repetitions must be greater than zero.");
        }

        Value = value;
    }

    public int Value { get; }

    public override string ToString() => Value.ToString();
}
