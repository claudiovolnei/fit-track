using FitTrack.Application.Exceptions;

namespace FitTrack.Application.Ports.Out;

/// <summary>
/// Preconditions the use cases state the same way, so a missing profile always gets the same answer.
/// The endpoint policy is the second line of defence; this is the one the use case owns.
/// </summary>
public static class CurrentUserExtensions
{
    public static void RequirePersonalTrainer(this ICurrentUser currentUser, string action)
    {
        if (!currentUser.IsPersonalTrainer)
        {
            throw new ForbiddenException($"Only personal trainers can {action}.");
        }
    }

    public static void RequireAdmin(this ICurrentUser currentUser, string action)
    {
        if (!currentUser.IsAdmin)
        {
            throw new ForbiddenException($"Only administrators can {action}.");
        }
    }

    /// <summary>
    /// The roster screens both profiles share: the personal trainer acts on their own athletes and
    /// the administrator on any of them, which is why each use case still states whose scope applies.
    /// </summary>
    public static void RequirePersonalTrainerOrAdmin(this ICurrentUser currentUser, string action)
    {
        if (!currentUser.IsPersonalTrainer && !currentUser.IsAdmin)
        {
            throw new ForbiddenException($"Only personal trainers or administrators can {action}.");
        }
    }
}
