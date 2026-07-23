namespace SetupDoctor.Core.Remediation;

public interface IRemediationAction
{
    string Id { get; }
    Task<RemediationPreview> PreviewAsync(RemediationContext context, CancellationToken cancellationToken);
    Task<RemediationExecutionResult> ExecuteAsync(RemediationContext context, CancellationToken cancellationToken);
    Task<RollbackResult> RollbackAsync(RemediationContext context, CancellationToken cancellationToken);
}
