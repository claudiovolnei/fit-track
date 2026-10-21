using FitTrack.Application.Commands;
using FitTrack.Application.Exceptions;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Common;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Application.UseCases;

/// <summary>
/// Links one athlete to one personal trainer. A personal trainer only ever ties an athlete to
/// themselves; the administrator names which trainer takes the athlete. Re-linking the same pair
/// answers nothing, and an athlete already on someone else's roster is refused, so the unique
/// index in the store never becomes the honest answer.
/// </summary>
public sealed class LinkAthlete : ILinkAthlete
{
    private readonly ICurrentUser _currentUser;
    private readonly ITrainerAthleteAssignmentRepository _links;
    private readonly IQueryAthletes _athletes;
    private readonly IClock _clock;
    private readonly IIdGenerator _idGenerator;

    public LinkAthlete(
        ICurrentUser currentUser,
        ITrainerAthleteAssignmentRepository links,
        IQueryAthletes athletes,
        IClock clock,
        IIdGenerator idGenerator)
    {
        _currentUser = currentUser;
        _links = links;
        _athletes = athletes;
        _clock = clock;
        _idGenerator = idGenerator;
    }

    public async Task ExecuteAsync(LinkAthleteCommand command, CancellationToken cancellationToken)
    {
        _currentUser.RequirePersonalTrainerOrAdmin("link athletes to a personal trainer");

        var athleteId = new UserId(command.AthleteId);

        if (!await _athletes.ExistsAsync(athleteId, cancellationToken))
        {
            throw new NotFoundException($"No athlete with id '{athleteId.Value}'.");
        }

        var trainerId = ResolveTrainerId(command.PersonalTrainerId);

        if (!await _athletes.ExistsPersonalTrainerAsync(trainerId, cancellationToken))
        {
            throw new NotFoundException($"No personal trainer with id '{trainerId.Value}'.");
        }

        var link = await _links.GetByAthleteIdAsync(athleteId, cancellationToken);

        if (link is null)
        {
            await _links.SaveAsync(
                TrainerAthleteAssignment.Link(
                    new TrainerAthleteAssignmentId(_idGenerator.NewId()),
                    trainerId,
                    athleteId,
                    _clock.UtcNow),
                cancellationToken);

            return;
        }

        if (link.TrainerId != trainerId)
        {
            throw new ConflictException(
                "roster.busy",
                "The athlete is already linked to another personal trainer.");
        }

        // Same pair again: the link is where it already was, which is what the caller asked for.
    }

    /// <summary>
    /// The personal trainer cannot name a target: the athlete becomes theirs, period. The
    /// administrator must name one, because they manage every roster at once.
    /// </summary>
    private UserId ResolveTrainerId(Guid? requested)
    {
        var named = requested is { } id && id != Guid.Empty;

        if (_currentUser.IsAdmin)
        {
            if (!named)
            {
                throw new DomainException(
                    "roster.trainer.required",
                    "The administrator must name the personal trainer the athlete joins.");
            }

            return new UserId(requested!.Value);
        }

        if (named && requested!.Value != _currentUser.Id.Value)
        {
            throw new ForbiddenException("A personal trainer only links athletes to themselves.");
        }

        return _currentUser.Id;
    }
}
