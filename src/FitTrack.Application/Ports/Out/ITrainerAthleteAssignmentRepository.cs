using FitTrack.Domain.Identity;

namespace FitTrack.Application.Ports.Out;

/// <summary>
/// Write-side access to the <see cref="TrainerAthleteAssignment"/> aggregate. One athlete answers to at
/// most one personal trainer, so <see cref="GetByAthleteIdAsync"/> is the whole lookup the use cases
/// need to state "yours" or "someone else's".
/// </summary>
public interface ITrainerAthleteAssignmentRepository
{
    Task<TrainerAthleteAssignment?> GetByAthleteIdAsync(
        UserId athleteId,
        CancellationToken cancellationToken);

    Task SaveAsync(TrainerAthleteAssignment assignment, CancellationToken cancellationToken);

    Task RemoveAsync(TrainerAthleteAssignment assignment, CancellationToken cancellationToken);
}
