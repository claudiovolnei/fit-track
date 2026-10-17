using FitTrack.Domain.Common;
using FitTrack.Domain.WorkoutCatalog;

namespace FitTrack.Unit.Domain;

public sealed class ExerciseTests
{
    [Fact]
    public void Create_StartsActive()
    {
        var exercise = Exercise.Create(ExerciseId.New(), new ExerciseName("Back Squat"), MuscleGroupId.New());

        Assert.True(exercise.IsActive);
    }

    [Fact]
    public void Create_WithoutMuscleGroup_Fails()
    {
        var exception = Assert.Throws<DomainException>(() =>
            Exercise.Create(ExerciseId.New(), new ExerciseName("Back Squat"), new MuscleGroupId(Guid.Empty)));

        Assert.Equal("exercise.muscle_group.required", exception.Code);
    }

    [Fact]
    public void ChangeMuscleGroup_WithoutMuscleGroup_Fails()
    {
        var exercise = Exercise.Create(ExerciseId.New(), new ExerciseName("Back Squat"), MuscleGroupId.New());

        var exception = Assert.Throws<DomainException>(() =>
            exercise.ChangeMuscleGroup(new MuscleGroupId(Guid.Empty)));

        Assert.Equal("exercise.muscle_group.required", exception.Code);
    }

    [Fact]
    public void Deactivate_ThenActivate_TogglesIsActive()
    {
        var exercise = Exercise.Create(ExerciseId.New(), new ExerciseName("Back Squat"), MuscleGroupId.New());

        exercise.Deactivate();
        Assert.False(exercise.IsActive);

        exercise.Activate();
        Assert.True(exercise.IsActive);
    }

    [Fact]
    public void Rename_ChangesTheName()
    {
        var exercise = Exercise.Create(ExerciseId.New(), new ExerciseName("Back Squat"), MuscleGroupId.New());

        exercise.Rename(new ExerciseName("Front Squat"));

        Assert.Equal("Front Squat", exercise.Name.Value);
    }

    [Fact]
    public void ChangeMuscleGroup_ChangesTheLink()
    {
        var exercise = Exercise.Create(ExerciseId.New(), new ExerciseName("Back Squat"), MuscleGroupId.New());
        var legs = MuscleGroupId.New();

        exercise.ChangeMuscleGroup(legs);

        Assert.Equal(legs, exercise.MuscleGroupId);
    }
}
