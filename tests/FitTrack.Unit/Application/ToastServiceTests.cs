using FitTrack.UI.Notifications;

namespace FitTrack.Unit.Application;

/// <summary>
/// The toast hub of the client: the screens report what happened and the host draws it. The rules
/// worth keeping are that a message leaves on its own and that a message without text never reaches
/// the screen.
/// </summary>
public sealed class ToastServiceTests
{
    [Fact]
    public void Show_KeepsTheMessageAndTellsTheHost()
    {
        var service = new ToastService();
        var changes = 0;
        service.Changed += () => changes++;

        var message = service.Error("Não foi possível concluir a ação.");

        Assert.Equal(ToastLevel.Error, message.Level);
        Assert.Equal("Não foi possível concluir a ação.", message.Text);
        Assert.Single(service.Messages);
        Assert.Equal(1, changes);
    }

    [Fact]
    public void Show_WithoutText_Fails()
    {
        var service = new ToastService();

        Assert.Throws<ArgumentException>(() => service.Show("   "));
    }

    [Fact]
    public void Dismiss_TakesTheMessageOffTheScreen()
    {
        var service = new ToastService();
        var message = service.Info("Treino iniciado.");

        service.Dismiss(message.Id);

        Assert.Empty(service.Messages);
    }

    [Fact]
    public void RemoveExpired_TakesOnlyTheMessagesWhoseTimeIsUp()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var service = new ToastService(clock);

        var quick = service.Show("Sumiu.", ToastLevel.Info, TimeSpan.FromSeconds(1));
        var slow = service.Show("Fica.", ToastLevel.Info, TimeSpan.FromMinutes(5));

        clock.Advance(TimeSpan.FromSeconds(2));

        Assert.Equal(1, service.RemoveExpired());
        Assert.Equal([slow.Id], service.Messages.Select(message => message.Id));
        Assert.DoesNotContain(quick.Id, service.Messages.Select(message => message.Id));
    }

    [Fact]
    public void Clear_TakesEveryMessageOffTheScreen()
    {
        var service = new ToastService();
        service.Info("Um.");
        service.Error("Dois.");

        service.Clear();

        Assert.Empty(service.Messages);
    }

    /// <summary>A clock the test moves by hand, so the expiry of a message is not a wait.</summary>
    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan amount) => _now = _now.Add(amount);
    }
}
