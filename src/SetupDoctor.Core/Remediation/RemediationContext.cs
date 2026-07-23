namespace SetupDoctor.Core.Remediation;

public sealed record RemediationContext(
    string RemediationId,
    string BackupDirectory,
    IReadOnlyDictionary<string, string> Parameters);
