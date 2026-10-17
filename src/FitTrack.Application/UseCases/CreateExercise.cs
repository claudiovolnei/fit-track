using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;
using FitTrack.Application.Exceptions;
using FitTrack.Application.Mapping;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.WorkoutCatalog;

namespace FitTrack.Application.UseCases;

/// <summary>Registers a new catalog exercise. Restricted to personal trainers.</summary>
public sealed class CreateExercise : ICreateExercise
{
    private readonly ICurrentUser _currentUser;
    private readonly IExerciseRepository _exercises;
    private readonly IMuscleGroupRepository _muscleGroups;
    private readonly IIdGenerator _idGenerator;

    public CreateExercise(
        ICurrentUser currentUser,
        IExerciseRepository exercises,
        IMuscleGroupRepository muscleGroups,
        IIdGenerator idGenerator)
    {
        _currentUser = currentUser;
        _exercises = exercises;
        _muscleGroups = muscleGroups;
        _idGenerator = idGenerator;
    }

    public async Task<ExerciseDto> ExecuteAsync(CreateExerciseCommand command, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsPersonalTrainer)
        {
            throw new ForbiddenException("Only personal trainers can manage the exercise catalog.");
        }

        // The aggregate states the "no muscle group" rule, so it runs before the lookup:
        // empty id is a malformed request (400), an unknown id is a missing reference (404).
        var exercise = Exercise.Create(
            new ExerciseId(_idGenerator.NewId()),
            new ExerciseName(command.Name),
            new MuscleGroupId(command.MuscleGroupId));

        var muscleGroup = await MuscleGroupLookup.RequireAsync(
            _muscleGroups,
            command.MuscleGroupId,
            cancellationToken);

        await _exercises.SaveAsync(exercise, cancellationToken);

        return exercise.ToDto(muscleGroup.Name);
    }
}
