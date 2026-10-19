using FitTrack.Application.Ports.Out;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Unit.Fakes;

/// <summary>In-memory write repository honouring the <see cref="IWorkoutSeriesRepository"/> contract.</summary>
internal sealed class InMemoryWorkoutSeriesRepository : IWorkoutSeriesRepository
{
    private readonly Dictionary<WorkoutSeriesId, WorkoutSeries> _byId = [];

    public IReadOnlyCollection<WorkoutSeries> All => _byId.Values;

    public Task<WorkoutSeries?> GetByIdAsync(WorkoutSeriesId id, CancellationToken cancellationToken) =>
        Task.FromResult(_byId.TryGetValue(id, out var series) ? series : null);

    public Task<bool> NameTakenAsync(
        WorkoutSeriesName name,
        FitTrack.Domain.Identity.UserId trainerId,
        WorkoutSeriesId? excluding,
        CancellationToken cancellationToken) =>
        Task.FromResult(_byId.Values.Any(series =>
            series.TrainerId == trainerId
            && series.Name.Value == name.Value
            && (excluding is null || series.Id != excluding.Value)));

    public Task<WorkoutSeriesWorkout?> GetWorkoutAsync(
        WorkoutSeriesWorkoutId id,
        CancellationToken cancellationToken) =>
        Task.FromResult(_byId.Values
            .SelectMany(series => series.Workouts)
            .FirstOrDefault(workout => workout.Id == id));

    public Task SaveAsync(WorkoutSeries series, CancellationToken cancellationToken)
    {
        _byId[series.Id] = series;

        return Task.CompletedTask;
    }
}
