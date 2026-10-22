using FitTrack.Application.Commands;

namespace FitTrack.Application.Ports.In;

/// <summary>
/// Prescribes a series that already exists: each athlete on the roster keeps their own assignment,
/// all following the same series the author left untouched.
/// </summary>
public interface IAssignWorkoutSeries
{
    Task ExecuteAsync(AssignWorkoutSeriesCommand command, CancellationToken cancellationToken);
}
