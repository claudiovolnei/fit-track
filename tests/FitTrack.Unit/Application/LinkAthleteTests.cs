using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;
using FitTrack.Application.Exceptions;
using FitTrack.Application.UseCases;
using FitTrack.Domain.Common;
using FitTrack.Domain.Identity;
using FitTrack.Unit.Fakes;

namespace FitTrack.Unit.Application;

/// <summary>
/// The roster rules on both use cases: who may name the trainer, what an already linked athlete
/// answers, and that unlinking only works for the trainer who owns the athlete (or the admin).
/// </summary>
public sealed class LinkAthleteTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 5, 12, 0, 0, TimeSpan.Zero);

    private readonly InMemoryTrainerAthleteAssignmentRepository _links = new();
    private readonly InMemoryAthleteQueries _athletes = new();
    private readonly FixedClock _clock = new(Now);
    private readonly SequentialIdGenerator _ids = new();
    private readonly UserId _trainer = UserId.New();
    private readonly UserId _otherTrainer = UserId.New();
    private readonly UserId _admin = UserId.New();
    private readonly UserId _athlete = UserId.New();
    private readonly UserId _anotherAthlete = UserId.New();

    public LinkAthleteTests()
    {
        _athletes.Seed(_athlete.Value, "athlete@fittrack.local");
        _athletes.Seed(_anotherAthlete.Value, "athlete2@fittrack.local");
        _athletes.SeedPersonalTrainer(_trainer.Value);
        _athletes.SeedPersonalTrainer(_otherTrainer.Value);
    }

    private LinkAthlete TrainerLink => new(
        StubCurrentUser.PersonalTrainer(_trainer),
        _links,
        _athletes,
        _clock,
        _ids);

    private LinkAthlete AdminLink => new(
        StubCurrentUser.Admin(_admin),
        _links,
        _athletes,
        _clock,
        _ids);

    private UnlinkAthlete TrainerUnlink => new(StubCurrentUser.PersonalTrainer(_trainer), _links);

    private UnlinkAthlete AdminUnlink => new(StubCurrentUser.Admin(_admin), _links);

    private ListAthletes TrainerList => new(
        StubCurrentUser.PersonalTrainer(_trainer),
        _athletes,
        _clock);

    [Fact]
    public async Task Link_PersonalTrainerLinksToThemselves()
    {
        await TrainerLink.ExecuteAsync(new LinkAthleteCommand(_athlete.Value), CancellationToken.None);

        Assert.Equal(_trainer.Value, _links.ByAthlete[_athlete.Value]);
    }

    [Fact]
    public async Task Link_SamePairAgain_IsIdempotent()
    {
        await TrainerLink.ExecuteAsync(new LinkAthleteCommand(_athlete.Value), CancellationToken.None);
        await TrainerLink.ExecuteAsync(new LinkAthleteCommand(_athlete.Value), CancellationToken.None);

        Assert.Single(_links.ByAthlete);
    }

    [Fact]
    public async Task Link_PersonalTrainerNamingAnotherTrainer_Fails()
    {
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            TrainerLink.ExecuteAsync(
                new LinkAthleteCommand(_athlete.Value, _otherTrainer.Value),
                CancellationToken.None));
    }

    [Fact]
    public async Task Link_AdminWithoutATrainer_Fails()
    {
        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            AdminLink.ExecuteAsync(new LinkAthleteCommand(_athlete.Value), CancellationToken.None));

        Assert.Equal("roster.trainer.required", exception.Code);
    }

    [Fact]
    public async Task Link_AdminNamesTheTrainer()
    {
        await AdminLink.ExecuteAsync(
            new LinkAthleteCommand(_anotherAthlete.Value, _otherTrainer.Value),
            CancellationToken.None);

        Assert.Equal(_otherTrainer.Value, _links.ByAthlete[_anotherAthlete.Value]);
    }

    [Fact]
    public async Task Link_AlreadyOnAnotherRoster_Fails()
    {
        _links.SeedLink(_otherTrainer.Value, _athlete.Value);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            TrainerLink.ExecuteAsync(new LinkAthleteCommand(_athlete.Value), CancellationToken.None));

        Assert.Equal("roster.busy", exception.Code);
    }

    [Fact]
    public async Task Link_UnknownAthlete_Fails()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            TrainerLink.ExecuteAsync(new LinkAthleteCommand(UserId.New().Value), CancellationToken.None));
    }

    [Fact]
    public async Task Link_UnknownTrainer_Fails()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            AdminLink.ExecuteAsync(
                new LinkAthleteCommand(_athlete.Value, UserId.New().Value),
                CancellationToken.None));
    }

    [Fact]
    public async Task Unlink_OwnAthlete()
    {
        _links.SeedLink(_trainer.Value, _athlete.Value);

        await TrainerUnlink.ExecuteAsync(new UnlinkAthleteCommand(_athlete.Value), CancellationToken.None);

        Assert.Empty(_links.ByAthlete);
    }

    [Fact]
    public async Task Unlink_SomeoneElsesAthlete_Fails()
    {
        _links.SeedLink(_otherTrainer.Value, _athlete.Value);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            TrainerUnlink.ExecuteAsync(new UnlinkAthleteCommand(_athlete.Value), CancellationToken.None));
    }

    [Fact]
    public async Task Unlink_WithoutALink_Fails()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            TrainerUnlink.ExecuteAsync(new UnlinkAthleteCommand(_athlete.Value), CancellationToken.None));
    }

    [Fact]
    public async Task Unlink_AdminRemovesFromAnyRoster()
    {
        _links.SeedLink(_otherTrainer.Value, _athlete.Value);

        await AdminUnlink.ExecuteAsync(new UnlinkAthleteCommand(_athlete.Value), CancellationToken.None);

        Assert.Empty(_links.ByAthlete);
    }

    [Fact]
    public async Task List_RosterScopeOfThePersonalTrainer()
    {
        _links.SeedLink(_trainer.Value, _athlete.Value);
        _athletes.Link(_trainer.Value, _athlete.Value);

        var athletes = await TrainerList.ExecuteAsync(CancellationToken.None);

        var only = Assert.Single(athletes);
        Assert.Equal(_athlete.Value, only.UserId);
    }

    [Fact]
    public async Task List_Candidates_AreOnlyTheUnlinkedOnes()
    {
        _links.SeedLink(_trainer.Value, _athlete.Value);
        _athletes.Link(_trainer.Value, _athlete.Value);

        var result = await TrainerList.ExecuteAsync(
            ListQuery.All,
            unlinkedOnly: true,
            CancellationToken.None);

        var only = Assert.Single(result.Items);
        Assert.Equal(_anotherAthlete.Value, only.UserId);
    }

    [Fact]
    public async Task List_AthleteIsDeniedTheRoster()
    {
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            new ListAthletes(StubCurrentUser.Athlete(_athlete), _athletes, _clock)
                .ExecuteAsync(CancellationToken.None));
    }
}
