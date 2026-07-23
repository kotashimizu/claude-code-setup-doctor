namespace SetupDoctor.Core.Diagnostics;

public sealed record DiagnosticContext(
    string SessionId,
    DateTimeOffset StartedAtUtc,
    IReadOnlyList<DiagnosticResult> PriorResults)
{
    public static DiagnosticContext Create(string sessionId, DateTimeOffset startedAt)
        => new(sessionId, startedAt, Array.Empty<DiagnosticResult>());

    public DiagnosticContext WithResult(DiagnosticResult result)
        => this with { PriorResults = [.. PriorResults, result] };

    public DiagnosticResult? GetPrior(string checkId)
        => PriorResults.FirstOrDefault(r => r.CheckId == checkId);
}
