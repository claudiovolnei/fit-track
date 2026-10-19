namespace FitTrack.Domain.WorkoutPlanning;

/// <summary>
/// The letter an athlete reads on the series sheet. It is derived from the one-based position of the
/// workout, which is why the aggregate renumbers the list when one leaves: the sheet stays A..E with
/// no hole. The values are explicit so the letter and the position cannot drift apart.
/// </summary>
public enum WorkoutLabel
{
    A = 1,
    B = 2,
    C = 3,
    D = 4,
    E = 5,
}
