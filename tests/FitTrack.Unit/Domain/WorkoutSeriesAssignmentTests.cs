using FitTrack.Domain.Common;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Unit.Domain;

public sealed class WorkoutSeriesAssignmentTests
{
    private static readonly UserId Athlete = UserId.New();
    private static readonly UserId Trainer = UserId.New();
    private static readonly DateTimeOffset AssignedAt = new(2026, 1, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Assign_WithoutAthlete_Fails()
    {
        var exception = Assert.Throws<DomainException>(() =>
            _ = WorkoutSeriesAssignment.Assign(
                WorkoutSeriesAssignmentId.New(),
                WorkoutSeriesId.New(),
                new UserId(Guid.Empty),
                Trainer,
                AssignedAt));

        Assert.Equal("workout_series.assignment.athlete.required", exception.Code);
    }

    [Fact]
    public void Assign_WithoutTrainer_Fails()
    {
        var exception = Assert.Throws<DomainException>(() =>
            _ = WorkoutSeriesAssignment.Assign(
                WorkoutSeriesAssignmentId.New(),
                WorkoutSeriesId.New(),
                Athlete,
                new UserId(Guid.Empty),
                AssignedAt));

        Assert.Equal("workout_series.trainer.required", exception.Code);
    }

    [Fact]
    public void Assign_SetsTheSixtyDayTerm()
    {
        var assignment = Assign();

        Assert.Equal(AssignedAt, assignment.AssignedAt);
        Assert.Equal(AssignedAt.AddDays(60), assignment.ExpiresAt);
        Assert.False(assignment.IsExpiredAt(AssignedAt));
        Assert.Equal(60, assignment.DaysRemaining(AssignedAt));
    }

    [Fact]
    public void IsExpiredAt_OneInstantBeforeTheTerm_IsFalse()
    {
        var assignment = Assign();

        Assert.False(assignment.IsExpiredAt(assignment.ExpiresAt.AddTicks(-1)));
    }

    [Fact]
    public void IsExpiredAt_AtTheTerm_IsTrue()
    {
        var assignment = Assign();

        Assert.True(assignment.IsExpiredAt(assignment.ExpiresAt));
    }

    [Fact]
    public void IsExpiredAt_AfterTheTerm_IsTrue()
    {
        var assignment = Assign();

        Assert.True(assignment.IsExpiredAt(AssignedAt.AddDays(61)));
    }

    [Fact]
    public void DaysRemaining_NeverGoesNegative()
    {
        var assignment = Assign();

        Assert.Equal(0, assignment.DaysRemaining(AssignedAt.AddDays(90)));
    }

    [Fact]
    public void Reassign_ChangesTheSeriesAndRestartsTheTerm()
    {
        var assignment = Assign();
        var other = WorkoutSeriesId.New();
        var later = AssignedAt.AddDays(10);

        assignment.Reassign(other, Trainer, later);

        Assert.Equal(other, assignment.SeriesId);
        Assert.Equal(later, assignment.AssignedAt);
        Assert.Equal(later.AddDays(60), assignment.ExpiresAt);
        Assert.Equal(60, assignment.DaysRemaining(later));
    }

    private static WorkoutSeriesAssignment Assign() =>
        WorkoutSeriesAssignment.Assign(
            WorkoutSeriesAssignmentId.New(),
            WorkoutSeriesId.New(),
            Athlete,
            Trainer,
            AssignedAt);
}
