namespace FitTrack.Domain.Identity;

/// <summary>
/// Identifies an account owned by the Identity context. Workout aggregates keep
/// only this id, never the account itself.
/// </summary>
public readonly record struct UserId(Guid Value)
{
    public static UserId New() => new(Guid.NewGuid());
}
