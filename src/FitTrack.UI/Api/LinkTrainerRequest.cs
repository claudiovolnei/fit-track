namespace FitTrack.UI.Api;

/// <summary>
/// Wire contract: linking an athlete to a trainer. The trainer id is optional because the personal
/// trainer links to themselves; the administrator is the one who names the trainer.
/// </summary>
public sealed record LinkTrainerRequest(Guid? PersonalTrainerId);
