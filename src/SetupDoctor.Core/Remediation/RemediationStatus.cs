namespace SetupDoctor.Core.Remediation;

public enum RemediationStatus
{
    Pending,
    Previewed,
    Confirmed,
    BackedUp,
    Executing,
    Verified,
    Failed,
    Cancelled,
    RollbackAvailable,
}
