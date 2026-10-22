using FitTrack.Application.Exceptions;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Identity;

namespace FitTrack.Application.UseCases;

/// <summary>
/// The destinations of a prescription, normalized the same way for both intents that prescribe: the
/// one that creates a series and the one that assigns an existing one. The single athlete leads the
/// merged list, duplications keep their first occurrence and an empty id counts as none; the
/// self-authored caller is their own only destination, and the athletes of the request are ignored.
/// <para>
/// The caller's order is what the flows read later — with one destination nothing changes, and with
/// several the first one holding an assignment donates the previous sheet.
/// </para>
/// </summary>
internal static class WorkoutSeriesDestinations
{
    /// <summary>The self-authored prescription: the athlete is the author and the destination.</summary>
    public static IReadOnlyList<UserId> ForCaller(UserId callerId) => [callerId];

    public static IReadOnlyList<UserId> Merge(Guid? athleteId, IReadOnlyList<Guid>? athleteIds)
    {
        List<Guid> merged = new();

        if (athleteId is { } single)
        {
            merged.Add(single);
        }

        if (athleteIds is { Count: > 0 })
        {
            merged.AddRange(athleteIds);
        }

        List<UserId> destinations = new();
        HashSet<Guid> seen = new();

        foreach (var candidate in merged)
        {
            if (candidate != Guid.Empty && seen.Add(candidate))
            {
                destinations.Add(new UserId(candidate));
            }
        }

        return destinations;
    }

    /// <summary>
    /// The roster rules what a trainer can prescribe: an athlete that does not exist, is unlinked or
    /// sits on another roster is refused the same way, without revealing whose roster the athlete is
    /// on. The whole list is validated before the caller saves anything.
    /// </summary>
    public static async Task RequirePrescribableAsync(
        UserId trainerId,
        IReadOnlyList<UserId> destinations,
        IQueryAthletes athletes,
        ITrainerAthleteAssignmentRepository links,
        CancellationToken cancellationToken)
    {
        foreach (var athlete in destinations)
        {
            if (!await athletes.ExistsAsync(athlete, cancellationToken))
            {
                throw new NotFoundException($"No athlete with id '{athlete.Value}'.");
            }

            var roster = await links.GetByAthleteIdAsync(athlete, cancellationToken);

            if (roster is null || roster.TrainerId != trainerId)
            {
                throw new ForbiddenException(
                    "A personal trainer only prescribes series for their own athletes.");
            }
        }
    }
}
