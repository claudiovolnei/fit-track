using FitTrack.Application.Commands;
using FitTrack.Application.Exceptions;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Identity;

namespace FitTrack.Application.UseCases;

/// <summary>
/// Undoes the roster link of one athlete. The personal trainer may only drop their own athletes;
/// the administrator drops from any roster. The link is the only thing removed: the series the
/// athlete follows stays, so the next trainer replaces it by recreating one.
/// </summary>
public sealed class UnlinkAthlete : IUnlinkAthlete
{
    private readonly ICurrentUser _currentUser;
    private readonly ITrainerAthleteAssignmentRepository _links;

    public UnlinkAthlete(ICurrentUser currentUser, ITrainerAthleteAssignmentRepository links)
    {
        _currentUser = currentUser;
        _links = links;
    }

    public async Task ExecuteAsync(UnlinkAthleteCommand command, CancellationToken cancellationToken)
    {
        _currentUser.RequirePersonalTrainerOrAdmin("unlink athletes from a personal trainer");

        var athleteId = new UserId(command.AthleteId);

        var link = await _links.GetByAthleteIdAsync(athleteId, cancellationToken)
            ?? throw new NotFoundException($"No athlete linked with id '{athleteId.Value}'.");

        if (!_currentUser.IsAdmin && link.TrainerId != _currentUser.Id)
        {
            throw new ForbiddenException("A personal trainer only removes their own athletes.");
        }

        await _links.RemoveAsync(link, cancellationToken);
    }
}
