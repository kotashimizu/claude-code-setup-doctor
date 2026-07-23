namespace SetupDoctor.Core.Remediation;

public sealed record RollbackResult(
    string RemediationId,
    bool Succeeded,
    string? ErrorMessage);
