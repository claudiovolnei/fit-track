using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Unit.Fakes;

/// <summary>In-memory write repository honouring the <see cref="IWorkoutPlanRepository"/> contract.</summary>
internal sealed class InMemoryWorkoutPlanRepository : IWorkoutPlanRepository
{
    private readonly Dictionary<WorkoutPlanId, WorkoutPlan> _byId = [];

    public IReadOnlyCollection<WorkoutPlan> All => _byId.Values;

    public Task<WorkoutPlan?> GetByIdAsync(WorkoutPlanId id, CancellationToken cancellationToken) =>
        Task.FromResult(_byId.TryGetValue(id, out var plan) ? plan : null);

    public Task<WorkoutPlan?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken) =>
        Task.FromResult(_byId.Values.FirstOrDefault(plan => plan.UserId == userId));

    public Task<WorkoutPlan?> GetByOptionIdAsync(WorkoutPlanOptionId optionId, CancellationToken cancellationToken) =>
        Task.FromResult(_byId.Values.FirstOrDefault(plan =>
            plan.Options.Any(option => option.Id == optionId)));

    public Task SaveAsync(WorkoutPlan plan, CancellationToken cancellationToken)
    {
        _byId[plan.Id] = plan;

        return Task.CompletedTask;
    }
}
