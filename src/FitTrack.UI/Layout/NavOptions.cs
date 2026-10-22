namespace FitTrack.UI.Layout;

/// <summary>Prioridade de um grupo de funcionalidades no menu mobile: atleta, depois personal, depois admin.</summary>
public enum NavGroup
{
    /// <summary>Estrutural (Início, Conta, acesso): não é um requisito de função.</summary>
    Core = 0,

    /// <summary>Treino do atleta: Hoje, Meus treinos.</summary>
    Athlete = 1,

    /// <summary>Gestão do personal: séries, atletas, catálogo.</summary>
    Trainer = 2,

    /// <summary>Administração: usuários.</summary>
    Admin = 3,
}

/// <summary>Quando um item do registro fica alcançável.</summary>
public enum NavAuth
{
    /// <summary>Aparece para qualquer visitante.</summary>
    Always = 0,

    /// <summary>Aparece para qualquer conta autenticada.</summary>
    Authenticated = 1,

    /// <summary>Aparece apenas para o visitante anônimo.</summary>
    Anonymous = 2,

    /// <summary>Aparece quando a conta tem pelo menos um papel de <see cref="NavItem.Roles"/>.</summary>
    Roles = 3,
}

/// <summary>Um destino de navegação, compartilhado entre a sidebar desktop e a tabbar mobile.</summary>
/// <param name="Section">Rótulo de agrupamento na sidebar e no painel "Mais" (vazio: fora de agrupamentos).</param>
/// <param name="Roles">Papéis exigidos; lido apenas quando <see cref="Auth"/> é <see cref="NavAuth.Roles"/>.</param>
/// <param name="Exact">Match por caminho inteiro, para rotas que são prefixo de outras.</param>
public sealed record NavItem(
    string Key,
    string Href,
    string Label,
    string Icon,
    NavGroup Group,
    string Section,
    NavAuth Auth,
    string[] Roles,
    bool Exact = false);

/// <summary>Um bloco estruturado da navegação com o mesmo rótulo.</summary>
public sealed record NavSection(string Label, IReadOnlyList<NavItem> Items);

/// <summary>Aba do painel "Mais": os itens que sobraram fora do teto de abas, pelo rótulo do agrupamento.</summary>
public sealed record SheetGroup(string Label, IReadOnlyList<NavItem> Items);

/// <summary>O que a tabbar mobile desenha: abas visíveis e, quando algo sobra, indicador do painel "Mais".</summary>
public sealed record TabbedNavigation(IReadOnlyList<NavItem> Tabs, bool HasMore);

/// <summary>
/// Single source of truth da navegação: a sidebar desktop e o menu mobile inferior derivam deste
/// registro, na ordem de prioridade atleta → personal → admin. Adicionar um destino aqui o torna
/// visível nos dois; o teto de abas mobile é aplicado por <see cref="Tabs"/>. O painel "Mais" mantém
/// a ordem do registro, agrupado pelo rótulo de <see cref="NavItem.Section"/>.
/// </summary>
public static class NavOptions
{
    /// <summary>Teto do menu inferior, contando o espaço que o painel "Mais" ocupa.</summary>
    public const int MaxTabs = 4;

    /// <summary>Chave da aba Conta: a tabbar substitui o ícone pelo avatar do usuário.</summary>
    public const string AccountKey = "account";

    private const string RoleUser = "User";
    private const string RolePersonalTrainer = "PersonalTrainer";
    private const string RoleAdmin = "Admin";

    // Ordem = prioridade: primeiro os destinos do atleta, depois da gestão do personal, depois
    // da administração. Os estruturais (Início, Conta, acesso) abrem/fecham a lista fixos.
    private static readonly NavItem[] Registry =
    [
        new("home", "/", "Início", "home", NavGroup.Core, "Visão geral", NavAuth.Always, [], Exact: true),
        new("today", "/today", "Hoje", "dumbbell", NavGroup.Athlete, "Treino", NavAuth.Roles, [RoleUser]),
        new("workouts", "/workouts", "Histórico", "history", NavGroup.Athlete, "Treino", NavAuth.Roles, [RoleUser]),
        new(
            "meu-plano", "/meu-plano", "Meu plano", "layers",
            NavGroup.Athlete, "Treino", NavAuth.Roles, [RoleUser]),
        new(
            "series", "/series", "Séries", "layers",
            NavGroup.Trainer, "Gestão", NavAuth.Roles, [RolePersonalTrainer]),
        new(
            "athletes", "/athletes", "Atletas", "users",
            NavGroup.Trainer, "Gestão", NavAuth.Roles, [RolePersonalTrainer, RoleAdmin]),
        new(
            "catalog", "/exercises/manage", "Catálogo", "library",
            NavGroup.Trainer, "Gestão", NavAuth.Roles, [RolePersonalTrainer]),
        new(
            "admin-users", "/admin/users", "Usuários", "users",
            NavGroup.Admin, "Administração", NavAuth.Roles, [RoleAdmin]),
        new(
            "admin-exercises", "/exercises", "Exercícios", "library",
            NavGroup.Admin, "Administração", NavAuth.Roles, [RoleAdmin, RolePersonalTrainer], Exact: true),
        new(AccountKey, "/account", "Conta", "user", NavGroup.Core, "", NavAuth.Authenticated, []),
        new("login", "/login", "Entrar", "user", NavGroup.Core, "", NavAuth.Anonymous, []),
        new("register", "/register", "Criar conta", "sparkles", NavGroup.Core, "", NavAuth.Anonymous, []),
    ];

    /// <summary>
    /// O menu inferior: Início sempre primeiro, funcionalidades por prioridade e Conta por último,
    /// sem passar de <see cref="MaxTabs"/>. Se nada couber, o espaço restante é o painel "Mais".
    /// </summary>
    public static TabbedNavigation Tabs(ICollection<string> roles, bool authenticated)
    {
        var available = Filtered(roles, authenticated);

        if (available.Count <= MaxTabs)
        {
            return new TabbedNavigation(available, HasMore: false);
        }

        // Distribuição: 3 abas fixas (Início + duas funcionalidades) e o último slot é o painel "Mais".
        return new TabbedNavigation(
            [.. available.Take(MaxTabs - 1)],
            HasMore: true);
    }

    /// <summary>Os itens que não couberam nas abas, agrupados pelos rótulos de treino, gestão e administração.</summary>
    public static IReadOnlyList<SheetGroup> Overflow(ICollection<string> roles, bool authenticated)
    {
        var available = Filtered(roles, authenticated);

        if (available.Count <= MaxTabs)
        {
            return [];
        }

        // Apenas os funcionais entram nos agrupamentos: os estruturais sem rótulo de seção
        // (Conta) são desenhados à parte, no fim do painel.
        return
        [
            .. available
                .Skip(MaxTabs - 1)
                .Where(item => item.Group is not NavGroup.Core)
                .GroupBy(item => item.Section)
                .Select(group => new SheetGroup(group.Key, [.. group])),
        ];
    }

    /// <summary>Seções da sidebar desktop, na mesma ordem e filtros do registro.</summary>
    public static IReadOnlyList<NavSection> Sidebar(ICollection<string> roles, bool authenticated)
    {
        var available = Filtered(roles, authenticated);

        var sections = new List<NavSection>();

        foreach (var grouping in available.Where(item => !string.IsNullOrEmpty(item.Section)).GroupBy(item => item.Section))
        {
            sections.Add(new NavSection(grouping.Key, [.. grouping]));
        }

        return sections;
    }

    private static IReadOnlyList<NavItem> Filtered(ICollection<string> roles, bool authenticated) =>
    [
        .. Registry.Where(item => item.IsAvailable(roles, authenticated)),
    ];

    private static bool IsAvailable(this NavItem item, ICollection<string> roles, bool authenticated) =>
        item.Auth switch
        {
            NavAuth.Always => true,
            NavAuth.Authenticated => authenticated,
            NavAuth.Anonymous => !authenticated,
            NavAuth.Roles => authenticated && item.Roles.Any(roles.Contains),
            _ => false,
        };
}
