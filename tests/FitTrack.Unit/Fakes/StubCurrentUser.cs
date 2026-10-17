using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Identity;

namespace FitTrack.Unit.Fakes;

/// <summary>Current-user stub with a fixed identity and profile.</summary>
internal sealed class StubCurrentUser(UserId id, bool isPersonalTrainer) : ICurrentUser
{
    public UserId Id { get; } = id;

    public bool IsPersonalTrainer { get; } = isPersonalTrainer;

    public static StubCurrentUser PersonalTrainer(UserId id) => new(id, isPersonalTrainer: true);

    public static StubCurrentUser Athlete(UserId id) => new(id, isPersonalTrainer: false);
}
