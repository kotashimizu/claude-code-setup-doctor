namespace SetupDoctor.Core.Diagnostics;

public sealed record DiagnosticResult(
    string CheckId,
    DiagnosticStatus Status,
    string SummaryKey,
    string DetailCode,
    IReadOnlyDictionary<string, string> SafeMetadata,
    TimeSpan Duration,
    DateTimeOffset CompletedAtUtc);
