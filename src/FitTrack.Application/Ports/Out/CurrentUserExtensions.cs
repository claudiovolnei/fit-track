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
}
