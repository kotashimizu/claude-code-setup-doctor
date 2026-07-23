using SetupDoctor.Core.Diagnostics;

namespace SetupDoctor.Core.Models;

public sealed record DiagnosticSession(
    string SessionId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    IReadOnlyList<DiagnosticResult> Results,
    OverallReadiness OverallReadiness);
