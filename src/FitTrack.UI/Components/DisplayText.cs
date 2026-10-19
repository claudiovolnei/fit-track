using System.Globalization;

namespace FitTrack.UI.Components;

/// <summary>
/// Formatação de apresentação usada por mais de uma tela. O domínio não conhece nenhuma destas
/// regras: são apenas a forma como o app fala com quem está treinando.
/// </summary>
public static class DisplayText
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>Primeiro nome legível a partir do e-mail que o token carrega.</summary>
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

    /// <summary>Até duas iniciais para o avatar redondo.</summary>
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

    /// <summary>Volume de treino em quilos, virando toneladas quando o número cresce.</summary>
    public static string Volume(decimal kilograms) =>
        kilograms >= 1000m
            ? $"{kilograms / 1000m:0.#} t"
            : $"{kilograms:0} kg";

    public static string LongDate(DateOnly date) =>
        date.ToDateTime(TimeOnly.MinValue).ToString("dddd, d 'de' MMMM", PtBr);

    public static string ShortMonth(DateOnly date) =>
        date.ToDateTime(TimeOnly.MinValue).ToString("MMM", PtBr).Replace(".", string.Empty);

    public static string Today =>
        DateTime.Today.ToString("dddd, d 'de' MMMM", PtBr);

    private static string Capitalize(string word) =>
        word.Length == 0
            ? word
            : char.ToUpper(word[0], PtBr) + word[1..].ToLower(PtBr);
}
