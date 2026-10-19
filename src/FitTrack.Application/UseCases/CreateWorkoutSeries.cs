using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Common;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Application.UseCases;

/// <summary>
/// Creates a reusable workout series. The name is unique per trainer, checked here so the caller gets
/// a stated rule instead of the raw unique-index error. Restricted to personal trainers.
/// </summary>
public sealed class CreateWorkoutSeries : ICreateWorkoutSeries
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkoutSeriesRepository _series;
    private readonly IQueryWorkoutSeries _query;
    private readonly IIdGenerator _idGenerator;

    public CreateWorkoutSeries(
        ICurrentUser currentUser,
        IWorkoutSeriesRepository series,
        IQueryWorkoutSeries query,
        IIdGenerator idGenerator)
    {
        _currentUser = currentUser;
        _series = series;
        _query = query;
        _idGenerator = idGenerator;
    }

    public async Task<WorkoutSeriesDto> ExecuteAsync(
        CreateWorkoutSeriesCommand command,
        CancellationToken cancellationToken)
    {
        _currentUser.RequirePersonalTrainer("create a workout series");

        var name = new WorkoutSeriesName(command.Name);

        if (await _series.NameTakenAsync(name, _currentUser.Id, excluding: null, cancellationToken))
        {
            throw new DomainException(
                "workout_series.name.duplicate",
                $"A workout series named '{name.Value}' already exists.");
        }

        var series = WorkoutSeries.Create(
            new WorkoutSeriesId(_idGenerator.NewId()),
            name,
            _currentUser.Id);

        await _series.SaveAsync(series, cancellationToken);

        return await WorkoutSeriesAccess.RequireDtoAsync(_query, series.Id, cancellationToken);
    }
}
