using FitTrack.Application.Commands;
using FitTrack.Application.Exceptions;
using FitTrack.Application.Ports.In;
using FitTrack.Application.UseCases;
using FitTrack.Domain.Common;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutCatalog;
using FitTrack.Domain.WorkoutPlanning;
using FitTrack.Unit.Fakes;

namespace FitTrack.Unit.Application;

/// <summary>
/// Assigning a series that already exists, and reading who follows it. Authorship rules the series
/// and the roster rules the destinations, the same way creation does.
/// </summary>
public sealed class AssignWorkoutSeriesTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 5, 12, 0, 0, TimeSpan.Zero);

    private readonly InMemoryWorkoutSeriesRepository _series = new();
    private readonly InMemoryWorkoutSeriesAssignmentRepository _assignments = new();
    private readonly InMemoryTrainerAthleteAssignmentRepository _links = new();
    private readonly InMemoryWorkoutSessionRepository _sessions = new();
    private readonly InMemoryExerciseRepository _exercises = new();
    private readonly InMemoryMuscleGroupRepository _muscleGroups = new();
    private readonly InMemoryAthleteQueries _athletes = new();
    private readonly InMemoryAccountPermissions _permissions = new();
    private readonly SequentialIdGenerator _ids = new();
    private readonly FixedClock _clock = new(Now);
    private readonly UserId _trainer = UserId.New();
    private readonly UserId _otherTrainer = UserId.New();
    private readonly UserId _athlete = UserId.New();
    private readonly UserId _secondAthlete = UserId.New();
    private readonly UserId _stranger = UserId.New();

    private readonly InMemoryWorkoutSeriesQueries _queries;

    public AssignWorkoutSeriesTests()
    {
        _queries = new(_series, _assignments, _exercises, _muscleGroups, _sessions);

        _athletes.Seed(_athlete.Value, "athlete@fittrack.local");
        _athletes.Seed(_secondAthlete.Value, "second@fittrack.local");
        _athletes.Seed(_stranger.Value, "stranger@fittrack.local");

        _links.SeedLink(_trainer.Value, _athlete.Value);
        _links.SeedLink(_trainer.Value, _secondAthlete.Value);
    }

    [Fact]
    public async Task Assign_AsTrainer_PointsEveryAthleteAtTheSeries()
    {
        var series = await SeedSeriesAsync(_trainer, "Hipertrofia A/B");

        await Assign(Trainer).ExecuteAsync(
            new AssignWorkoutSeriesCommand(series.Id.Value, AthleteIds: [_athlete.Value, _secondAthlete.Value]),
            CancellationToken.None);

        Assert.Equal(2, _assignments.All.Count);
        Assert.All(_assignments.All, assignment =>
        {
            Assert.Equal(series.Id.Value, assignment.SeriesId.Value);
            Assert.True(assignment.IsOpen);
        });
    }

    [Fact]
    public async Task Assign_WithTheSingleAthlete_KeepsTheSingularDestination()
    {
        var series = await SeedSeriesAsync(_trainer);

        await Assign(Trainer).ExecuteAsync(
            new AssignWorkoutSeriesCommand(series.Id.Value, _athlete.Value),
            CancellationToken.None);

        var link = Assert.Single(_assignments.All);

        Assert.Equal(series.Id.Value, link.SeriesId.Value);
    }

    [Fact]
    public async Task Assign_UnknownSeries_Fails()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            Assign(Trainer).ExecuteAsync(
                new AssignWorkoutSeriesCommand(Guid.NewGuid(), _athlete.Value),
                CancellationToken.None));

        Assert.Empty(_assignments.All);
    }

    [Fact]
    public async Task Assign_SeriesOfAnotherTrainer_Fails()
    {
        var foreign = await SeedSeriesAsync(_otherTrainer, "Do outro");

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            Assign(Trainer).ExecuteAsync(
                new AssignWorkoutSeriesCommand(foreign.Id.Value, _athlete.Value),
                CancellationToken.None));

        Assert.Empty(_assignments.All);
    }

    [Fact]
    public async Task Assign_AthleteOfAnotherRoster_Fails_WithoutSavingAnything()
    {
        var series = await SeedSeriesAsync(_trainer);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            Assign(Trainer).ExecuteAsync(
                new AssignWorkoutSeriesCommand(
                    series.Id.Value,
                    AthleteIds: [_athlete.Value, _stranger.Value]),
                CancellationToken.None));

        // All-or-nothing: the valid destination is never linked when one of them fails.
        Assert.Empty(_assignments.All);
    }

    [Fact]
    public async Task Assign_UnknownAthlete_Fails()
    {
        var series = await SeedSeriesAsync(_trainer);
        var ghost = UserId.New().Value;

        await Assert.ThrowsAsync<NotFoundException>(() =>
            Assign(Trainer).ExecuteAsync(
                new AssignWorkoutSeriesCommand(series.Id.Value, AthleteIds: [ghost]),
                CancellationToken.None));
    }

    [Fact]
    public async Task Assign_OverACycleTheAthleteAlreadyFollows_ReassignsTheLink()
    {
        var first = await Create(Trainer).ExecuteAsync(
            new CreateWorkoutSeriesCommand("Ciclo atual", _athlete.Value),
            CancellationToken.None);
        var link = await _assignments.GetByUserIdAsync(_athlete, CancellationToken.None);
        Assert.NotNull(link);

        var second = await SeedSeriesAsync(_trainer, "Próximo ciclo");

        await Assign(Trainer).ExecuteAsync(
            new AssignWorkoutSeriesCommand(second.Id.Value, _athlete.Value),
            CancellationToken.None);

        Assert.Single(_assignments.All);
        Assert.Equal(second.Id.Value, link.SeriesId.Value);
        Assert.True(link.IsOpen);
        Assert.Equal(Now, link.AssignedAt);
    }

    [Fact]
    public async Task Assign_DriverIgnoresDuplicationsInTheList()
    {
        var series = await SeedSeriesAsync(_trainer);
        var sameId = _athlete.Value;

        await Assign(Trainer).ExecuteAsync(
            new AssignWorkoutSeriesCommand(
                series.Id.Value,
                AthleteIds: [sameId, _secondAthlete.Value, sameId, Guid.Empty]),
            CancellationToken.None);

        Assert.Equal(2, _assignments.All.Count);
    }

    [Fact]
    public async Task Assign_AsAReleasedAthlete_PointsAtTheirOwnSeries_IgnoresTheOthers()
    {
        var created = await Create(ReleasedAthlete).ExecuteAsync(
            new CreateWorkoutSeriesCommand("Meu plano"),
            CancellationToken.None);

        await Assign(ReleasedAthlete).ExecuteAsync(
            new AssignWorkoutSeriesCommand(
                created.Id,
                AthleteIds: [_stranger.Value, _secondAthlete.Value]),
            CancellationToken.None);

        var link = await _assignments.GetByUserIdAsync(_athlete, CancellationToken.None);

        Assert.NotNull(link);
        Assert.Equal(created.Id, link.SeriesId.Value);
        Assert.Single(_assignments.All);
    }

    [Fact]
    public async Task Assign_WithoutTheRelease_Fails()
    {
        var series = await SeedSeriesAsync(_trainer);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            Assign(Athlete).ExecuteAsync(
                new AssignWorkoutSeriesCommand(series.Id.Value),
                CancellationToken.None));
    }

    [Fact]
    public async Task Assign_WithoutAnyDestination_Fails()
    {
        var series = await SeedSeriesAsync(_trainer);

        await Assert.ThrowsAsync<InvalidCommandException>(() =>
            Assign(Trainer).ExecuteAsync(new AssignWorkoutSeriesCommand(series.Id.Value), CancellationToken.None));

        await Assert.ThrowsAsync<InvalidCommandException>(() =>
            Assign(Trainer).ExecuteAsync(
                new AssignWorkoutSeriesCommand(series.Id.Value, AthleteIds: [Guid.Empty]),
                CancellationToken.None));

        Assert.Empty(_assignments.All);
    }

    [Fact]
    public async Task Followers_ListTheAthletesWithTheirTerm()
    {
        var series = await SeedSeriesAsync(_trainer);

        await Assign(Trainer).ExecuteAsync(
            new AssignWorkoutSeriesCommand(
                series.Id.Value,
                AthleteIds: [_secondAthlete.Value, _athlete.Value]),
            CancellationToken.None);

        _queries.SeedUser(_athlete.Value, "athlete@fittrack.local", "Atleta Um");
        _queries.SeedUser(_secondAthlete.Value, "second@fittrack.local", null);

        var followers = await Followers(Trainer).ExecuteAsync(series.Id.Value, CancellationToken.None);

        Assert.Equal(2, followers.Count);

        var first = followers.Single(row => row.UserId == _athlete.Value);
        Assert.Equal("Atleta Um", first.Name);
        Assert.Equal("athlete@fittrack.local", first.Email);
        Assert.True(first.IsOpen);
        Assert.Equal(60, first.DaysRemaining);

        var second = followers.Single(row => row.UserId == _secondAthlete.Value);
        Assert.Null(second.Name);
        Assert.Equal("second@fittrack.local", second.Email);
    }

    [Fact]
    public async Task Followers_WhenNobodyFollows_AnswersAnEmptyList()
    {
        var series = await SeedSeriesAsync(_trainer);

        var followers = await Followers(Trainer).ExecuteAsync(series.Id.Value, CancellationToken.None);

        Assert.Empty(followers);
    }

    [Fact]
    public async Task Followers_SeriesOfAnotherTrainer_AnswersAsIfItDidNotExist()
    {
        var foreign = await SeedSeriesAsync(_otherTrainer);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            Followers(Trainer).ExecuteAsync(foreign.Id.Value, CancellationToken.None));
    }

    [Fact]
    public async Task Followers_UnknownSeries_Fails()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            Followers(Trainer).ExecuteAsync(Guid.NewGuid(), CancellationToken.None));
    }

    private StubCurrentUser Trainer => StubCurrentUser.PersonalTrainer(_trainer);

    private StubCurrentUser OtherTrainer => StubCurrentUser.PersonalTrainer(_otherTrainer);

    private StubCurrentUser Athlete => StubCurrentUser.Athlete(_athlete);

    /// <summary>The athlete the administrator released to build their own plan.</summary>
    private StubCurrentUser ReleasedAthlete
    {
        get
        {
            _permissions.Release(_athlete);

            return Athlete;
        }
    }

    private ICreateWorkoutSeries Create(StubCurrentUser user) =>
        new CreateWorkoutSeries(user, _series, _queries, _athletes, _links, _assignments, _clock, _ids, _permissions);

    private IAssignWorkoutSeries Assign(StubCurrentUser user) =>
        new AssignWorkoutSeries(user, _series, _athletes, _links, _assignments, _clock, _ids, _permissions);

    private IListWorkoutSeriesFollowers Followers(StubCurrentUser user) =>
        new ListWorkoutSeriesFollowers(user, _queries, _permissions, _clock);

    private async Task<WorkoutSeries> SeedSeriesAsync(UserId trainer, string name = "Hipertrofia A/B/C")
    {
        var series = WorkoutSeries.Create(new WorkoutSeriesId(_ids.NewId()), new WorkoutSeriesName(name), trainer);

        await _series.SaveAsync(series, CancellationToken.None);

        return series;
    }
}
