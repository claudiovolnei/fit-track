using FitTrack.UI.Layout;

namespace FitTrack.Unit.Layout;

/// <summary>The profile matrix the mobile tab bar must satisfy: up to four tabs, "Mais" beyond that.</summary>
public sealed class NavOptionsTests
{
    private const string User = "User";
    private const string Trainer = "PersonalTrainer";
    private const string Admin = "Admin";

    [Fact]
    public void Tabs_Anonymous_ShowsTheEntryRoutes()
    {
        var tabs = NavOptions.Tabs([], authenticated: false);

        Assert.Equal(["home", "login", "register"], Keys(tabs.Tabs));
        Assert.False(tabs.HasMore);
    }

    [Fact]
    public void Tabs_AthleteOnly_FitsTheCapWithoutTheSheet()
    {
        var tabs = NavOptions.Tabs([User], authenticated: true);

        Assert.Equal(["home", "today", "workouts", "account"], Keys(tabs.Tabs));
        Assert.False(tabs.HasMore);
    }

    [Fact]
    public void Tabs_AdminOnly_FitsTheCapWithoutTheSheet()
    {
        var tabs = NavOptions.Tabs([Admin], authenticated: true);

        Assert.Equal(["home", "admin-users", "admin-exercises", "account"], Keys(tabs.Tabs));
        Assert.False(tabs.HasMore);
    }

    [Fact]
    public void Tabs_TrainerOnly_KeepsTheTopAndMovesTheRestToTheSheet()
    {
        var tabs = NavOptions.Tabs([Trainer], authenticated: true);

        Assert.Equal(["home", "series", "athletes"], Keys(tabs.Tabs));
        Assert.True(tabs.HasMore);

        var sheet = NavOptions.Overflow([Trainer], authenticated: true);
        Assert.Equal(
            [
                ("Gestão", ["catalog"]),
                ("Administração", ["admin-exercises"]),
            ],
            SheetKeys(sheet));
    }

    [Fact]
    public void Tabs_TrainerAndAdmin_AdminFeaturesLandAfterTheTrainerOnes()
    {
        var tabs = NavOptions.Tabs([Trainer, Admin], authenticated: true);

        Assert.Equal(["home", "series", "athletes"], Keys(tabs.Tabs));
        Assert.True(tabs.HasMore);

        var sheet = NavOptions.Overflow([Trainer, Admin], authenticated: true);
        Assert.Equal(
            [
                ("Gestão", ["catalog"]),
                ("Administração", ["admin-users", "admin-exercises"]),
            ],
            SheetKeys(sheet));
    }

    [Fact]
    public void Tabs_EveryRole_AthleteFeaturesComeFirst()
    {
        var tabs = NavOptions.Tabs([User, Trainer, Admin], authenticated: true);

        Assert.Equal(["home", "today", "workouts"], Keys(tabs.Tabs));
        Assert.True(tabs.HasMore);
    }

    [Fact]
    public void Tabs_AnyProfile_NeverExceedsTheCap()
    {
        var profiles = new(
            bool Authenticated,
            string[] Roles)[]
        {
            (false, []),
            (true, [User]),
            (true, [Trainer]),
            (true, [Admin]),
            (true, [Trainer, Admin]),
            (true, [User, Admin]),
            (true, [User, Trainer, Admin]),
        };

        foreach (var (authenticated, roles) in profiles)
        {
            var tabs = NavOptions.Tabs(roles, authenticated);

            Assert.True(tabs.Tabs.Count <= NavOptions.MaxTabs, $"Profile [{string.Join(", ", roles)}]");
        }
    }

    [Fact]
    public void Sidebar_TrainerOnly_KeepsTheSectionLabels()
    {
        var sections = NavOptions.Sidebar([Trainer], authenticated: true);

        Assert.Equal(["Visão geral", "Gestão", "Administração"], Ids(sections));
        Assert.Equal(["home"], Keys(sections[0].Items));
        Assert.Equal(["series", "athletes", "catalog"], Keys(sections[1].Items));
        Assert.Equal(["admin-exercises"], Keys(sections[2].Items));
    }

    private static string[] Keys(IReadOnlyList<NavItem> items) => [.. items.Select(item => item.Key)];

    private static (string Label, string[] Keys)[] SheetKeys(IReadOnlyList<SheetGroup> groups) =>
    [
        .. groups.Select(group => (group.Label, Keys(group.Items))),
    ];

    private static string[] Ids(IReadOnlyList<NavSection> sections) => [.. sections.Select(section => section.Label)];
}
