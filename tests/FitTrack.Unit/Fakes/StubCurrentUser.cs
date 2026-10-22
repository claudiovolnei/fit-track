using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Identity;

namespace FitTrack.Unit.Fakes;

/// <summary>Current-user stub with a fixed identity and profile.</summary>
internal sealed class StubCurrentUser(
    UserId id,
    bool isPersonalTrainer,
    bool isAdmin = false,
    bool isAthlete = false) : ICurrentUser
{
    public UserId Id { get; } = id;

    public bool IsPersonalTrainer { get; } = isPersonalTrainer;

    public bool IsAdmin { get; } = isAdmin;

    public bool IsAthlete { get; } = isAthlete;

    public static StubCurrentUser Admin(UserId id) => new(id, isPersonalTrainer: false, isAdmin: true);

    public static StubCurrentUser PersonalTrainer(UserId id) => new(id, isPersonalTrainer: true);

    public static StubCurrentUser Athlete(UserId id) => new(id, isPersonalTrainer: false, isAthlete: true);
}
