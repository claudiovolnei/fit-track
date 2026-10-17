using FitTrack.Domain.Common;

namespace FitTrack.Domain.WorkoutExecution;

/// <summary>Value object for the weight moved in a performed set, in kilograms.</summary>
public readonly record struct Load
{
    public Load(decimal kilograms)
    {
        if (kilograms < 0)
        {
            throw new DomainException("set.load.negative", "Load cannot be negative.");
        }

        Kilograms = kilograms;
    }

    public decimal Kilograms { get; }

    public override string ToString() => $"{Kilograms} kg";
}
