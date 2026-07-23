namespace SetupDoctor.Core.Remediation;

public interface IRemediationOrchestrator
{
    Task<IReadOnlyList<RemediationExecutionResult>> ExecuteAsync(
        IReadOnlyList<RemediationPlanItem> plan,
        IProgress<RemediationExecutionResult>? progress,
        CancellationToken cancellationToken);
}
