using FitTrack.Domain.Common;

namespace FitTrack.Domain.WorkoutExecution;

/// <summary>
/// What the athlete wrote about the workout. Free text, bounded so the history stays readable; the
/// absence of a note is a null outside the value object, not an empty text inside it.
/// </summary>
public readonly record struct WorkoutNotes
{
    /// <summary>How many characters a note may have.</summary>
    public const int MaxLength = 500;

    public WorkoutNotes(string value)
    {
        var text = value ?? string.Empty;

        if (text.Length > MaxLength)
        {
            throw new DomainException(
                "workout_session.notes.too_long",
                $"The note about the workout must have at most {MaxLength} characters.");
        }

        Value = text;
    }

    /// <summary>The text the athlete wrote.</summary>
    public string Value { get; }

    public override string ToString() => Value;
}
