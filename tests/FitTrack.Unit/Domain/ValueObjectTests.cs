using FitTrack.Domain.Common;
using FitTrack.Domain.WorkoutCatalog;
using FitTrack.Domain.WorkoutExecution;

namespace FitTrack.Unit.Domain;

public sealed class ValueObjectTests
{
    [Fact]
    public void Load_Negative_Fails()
    {
        Assert.Throws<DomainException>(() => _ = new Load(-0.5m));
    }

    [Fact]
    public void Load_Zero_IsAllowed()
    {
        var load = new Load(0m);

        Assert.Equal(0m, load.Kilograms);
    }

    [Fact]
    public void Repetitions_NotPositive_Fails()
    {
        Assert.Throws<DomainException>(() => _ = new Repetitions(0));
    }

    [Fact]
    public void ExerciseName_Blank_Fails()
    {
        Assert.Throws<DomainException>(() => _ = new ExerciseName("   "));
    }

    [Fact]
    public void ExerciseName_TrimsSurroundingWhitespace()
    {
        var name = new ExerciseName("  Deadlift  ");

        Assert.Equal("Deadlift", name.Value);
    }

    [Fact]
    public void MuscleGroupName_TooLong_Fails()
    {
        var tooLong = new string('x', MuscleGroupName.MaxLength + 1);

        Assert.Throws<DomainException>(() => _ = new MuscleGroupName(tooLong));
    }

    [Fact]
    public void MuscleGroupName_TooShort_Fails()
    {
        var exception = Assert.Throws<DomainException>(() => _ = new MuscleGroupName(" A "));

        Assert.Equal("muscle_group.name.required", exception.Code);
    }

    [Fact]
    public void MuscleGroupName_TrimsSurroundingWhitespace()
    {
        var name = new MuscleGroupName("  Chest  ");

        Assert.Equal("Chest", name.Value);
    }
}
