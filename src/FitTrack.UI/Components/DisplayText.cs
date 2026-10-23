using System.Globalization;

namespace FitTrack.UI.Components;

/// <summary>
/// Presentation formatting shared by more than one screen. The domain knows none of these
/// rules: they are just the way the app talks to whoever is training.
/// </summary>
public static class DisplayText
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>Readable first name from the e-mail the token carries.</summary>
    public static string NameFromEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return "Atleta";
        }

        var local = email.Split('@')[0].Replace('.', ' ').Replace('_', ' ').Replace('-', ' ');
        var words = local.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return string.Join(' ', words.Select(Capitalize));
    }

    /// <summary>Up to two initials for the round avatar.</summary>
    public static string Initials(string? email)
    {
        var name = NameFromEmail(email);
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return parts.Length switch
        {
            0 => "FT",
            1 => parts[0][..1].ToUpper(PtBr),
            _ => $"{parts[0][0]}{parts[^1][0]}".ToUpper(PtBr),
        };
    }

    public static string Duration(int seconds)
    {
        var elapsed = TimeSpan.FromSeconds(seconds);

        return elapsed.TotalHours >= 1
            ? elapsed.ToString(@"h\:mm\:ss", PtBr)
            : elapsed.ToString(@"m\:ss", PtBr);
    }

    /// <summary>Training volume in kilograms, turning into tonnes once the number grows.</summary>
    public static string Volume(decimal kilograms) =>
        kilograms >= 1000m
            ? $"{kilograms / 1000m:0.#} t"
            : $"{kilograms:0} kg";

    /// <summary>A representative load in kilograms with up to two decimals; never turns into tonnes.</summary>
    public static string Load(decimal kilograms) => $"{kilograms:0.##} kg";

    /// <summary>The same load with an explicit sign, for the per-exercise increase against the prescription.</summary>
    public static string SignedLoad(decimal kilograms) =>
        kilograms > 0m
            ? $"+{Load(kilograms)}"
            : kilograms < 0m
                ? $"−{Load(-kilograms)}"
                : "0 kg";

    /// <summary>The same volume with an explicit sign, for the increase against the prescription.</summary>
    public static string SignedVolume(decimal kilograms) =>
        kilograms > 0m
            ? $"+{Volume(kilograms)}"
            : kilograms < 0m
                ? $"−{Volume(-kilograms)}"
                : "0 kg";

    /// <summary>The percentage with an explicit sign, rounded like the API does.</summary>
    public static string SignedPercent(decimal percent) =>
        percent > 0m
            ? $"+{percent:0.#}%"
            : percent < 0m
                ? $"−{(-percent):0.#}%"
                : "0%";

    public static string LongDate(DateOnly date) =>
        date.ToDateTime(TimeOnly.MinValue).ToString("dddd, d 'de' MMMM", PtBr);

    public static string ShortMonth(DateOnly date) =>
        date.ToDateTime(TimeOnly.MinValue).ToString("MMM", PtBr).Replace(".", string.Empty);

    public static string Today =>
        DateTime.Today.ToString("dddd, d 'de' MMMM", PtBr);

    /// <summary>
    /// The profiles the app knows, from strongest to weakest. The API uses the Identity names,
    /// in English; the order is the one the header and the users screen present.
    /// </summary>
    public static IReadOnlyList<string> Roles { get; } = ["Admin", "PersonalTrainer", "User"];

    /// <summary>
    /// The profile name as the app writes it. The physical education professional's profile is "Personal",
    /// the same name the Today screen uses for whoever prescribed the series.
    /// </summary>
    public static string RoleLabel(string role) => role switch
    {
        "Admin" => "Administrador",
        "PersonalTrainer" => "Personal",
        "User" => "Atleta",
        _ => role,
    };

    /// <summary>
    /// The levels of intensity of a workout, in the order the athlete picks them. The value is what
    /// travels to the API; the label is what the screen shows.
    /// </summary>
    public static IReadOnlyList<(string Value, string Label)> Intensities { get; } =
    [
        ("leve", "Leve"),
        ("medio", "Médio"),
        ("moderado", "Moderado"),
        ("intenso", "Intenso"),
        ("muitoIntenso", "Muito intenso"),
    ];

    /// <summary>
    /// The level of a finished workout as the screen names it. A session finished before the athlete
    /// started answering has no level, and the screen says nothing about it.
    /// </summary>
    public static string IntensityLabel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        foreach (var option in Intensities)
        {
            if (string.Equals(option.Value, value, StringComparison.OrdinalIgnoreCase))
            {
                return option.Label;
            }
        }

        return value;
    }

    private static string Capitalize(string word) =>
        word.Length == 0
            ? word
            : char.ToUpper(word[0], PtBr) + word[1..].ToLower(PtBr);
}
