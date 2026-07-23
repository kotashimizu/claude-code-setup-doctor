namespace SetupDoctor.Core.Diagnostics;

public enum DiagnosticStatus
{
    Pass,
    Warning,
    Fail,
    Repairable,
    UserAction,
    ITAction,
    NotApplicable,
    Unknown,
}
