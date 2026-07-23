namespace SetupDoctor.Core.Models;

public enum OverallReadiness
{
    Ready,
    ReadyWithRecommendations,
    Repairable,
    UserActionRequired,
    ITActionRequired,
    Unsupported,
    Unknown,
}
