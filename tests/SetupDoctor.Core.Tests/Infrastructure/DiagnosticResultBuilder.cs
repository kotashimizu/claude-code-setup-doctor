using SetupDoctor.Core.Diagnostics;

namespace SetupDoctor.Core.Tests.Infrastructure;

public static class DiagnosticResultBuilder
{
    public static DiagnosticResult Build(
        string checkId,
        DiagnosticStatus status,
        string summaryKey = "test.summary",
        string detailCode = "TEST_DETAIL",
        IReadOnlyDictionary<string, string>? metadata = null)
        => new(
            CheckId: checkId,
            Status: status,
            SummaryKey: summaryKey,
            DetailCode: detailCode,
            SafeMetadata: metadata ?? new Dictionary<string, string>(),
            Duration: TimeSpan.FromMilliseconds(10),
            CompletedAtUtc: DateTimeOffset.UtcNow);

    public static DiagnosticResult Pass(string checkId) => Build(checkId, DiagnosticStatus.Pass);
    public static DiagnosticResult Fail(string checkId) => Build(checkId, DiagnosticStatus.Fail);
    public static DiagnosticResult Warning(string checkId) => Build(checkId, DiagnosticStatus.Warning);
    public static DiagnosticResult Repairable(string checkId) => Build(checkId, DiagnosticStatus.Repairable);
    public static DiagnosticResult UserAction(string checkId) => Build(checkId, DiagnosticStatus.UserAction);
    public static DiagnosticResult ITAction(string checkId) => Build(checkId, DiagnosticStatus.ITAction);
    public static DiagnosticResult Unknown(string checkId) => Build(checkId, DiagnosticStatus.Unknown);
}
