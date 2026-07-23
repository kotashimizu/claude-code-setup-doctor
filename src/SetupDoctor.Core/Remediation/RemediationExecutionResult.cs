namespace SetupDoctor.Core.Remediation;

public sealed record RemediationExecutionResult(
    string RemediationId,
    RemediationStatus Status,
    string? ErrorMessage,
    bool RollbackAvailable);
