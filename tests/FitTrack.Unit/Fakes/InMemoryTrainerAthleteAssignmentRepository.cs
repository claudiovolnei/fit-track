using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Identity;

namespace FitTrack.Unit.Fakes;

/// <summary>Write-side fake for the roster link, keyed by the athlete: the "one trainer per athlete" rule.</summary>
internal sealed class InMemoryTrainerAthleteAssignmentRepository : ITrainerAthleteAssignmentRepository
{
    private sealed class Entry(Guid trainerId, Guid athleteId)
    {
        public Guid TrainerId { get; } = trainerId;

        public Guid AthleteId { get; } = athleteId;
    }

    private readonly List<Entry> _assignments = [];

    public IReadOnlyDictionary<Guid, Guid> ByAthlete =>
        _assignments.ToDictionary(assignment => assignment.AthleteId, assignment => assignment.TrainerId);

    public Task<TrainerAthleteAssignment?> GetByAthleteIdAsync(
        UserId athleteId,
        CancellationToken cancellationToken) =>
        Task.FromResult(_assignments
                .Where(assignment => assignment.AthleteId == athleteId.Value)
                .Select(assignment => TrainerAthleteAssignment.Link(
                    new TrainerAthleteAssignmentId(assignment.AthleteId),
                    new UserId(assignment.TrainerId),
                    athleteId,
                    DateTimeOffset.UnixEpoch))
                .FirstOrDefault());

    /// <summary>Direct seed, for the arranges that assume the roster in place without a use case.</summary>
    public void SeedLink(Guid trainerId, Guid athleteId)
    {
        _assignments.RemoveAll(candidate => candidate.AthleteId == athleteId);
        _assignments.Add(new Entry(trainerId, athleteId));
    }

    public Task SaveAsync(TrainerAthleteAssignment assignment, CancellationToken cancellationToken)
    {
        _assignments.RemoveAll(candidate => candidate.AthleteId == assignment.AthleteId.Value);
        _assignments.Add(new Entry(assignment.TrainerId.Value, assignment.AthleteId.Value));

        return Task.CompletedTask;
    }

    public Task RemoveAsync(TrainerAthleteAssignment assignment, CancellationToken cancellationToken)
    {
        _assignments.RemoveAll(candidate => candidate.AthleteId == assignment.AthleteId.Value);

        return Task.CompletedTask;
    }
}
