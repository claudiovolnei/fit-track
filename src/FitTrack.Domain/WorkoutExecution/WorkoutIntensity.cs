namespace FitTrack.Domain.WorkoutExecution;

/// <summary>
/// How intense the athlete says the workout was. A closed list: the history compares one session
/// with another, so the level cannot be free text. The numeric values are explicit because the
/// enum is stored as a number in the database — reordering the members must never change what an
/// old row means.
/// </summary>
public enum WorkoutIntensity
{
    /// <summary>A light session.</summary>
    Leve = 1,

    /// <summary>Easy, below the usual effort.</summary>
    Medio = 2,

    /// <summary>The usual effort.</summary>
    Moderado = 3,

    /// <summary>A hard session.</summary>
    Intenso = 4,

    /// <summary>A very hard session.</summary>
    MuitoIntenso = 5,
}
