namespace SetupDoctor.Core.Remediation;

public sealed record RemediationPlanItem(
    string RemediationId,
    string Title,
    string Target,
    string BeforeSummary,
    string AfterSummary,
    bool RequiresElevation,
    bool RequiresNewProcess,
    bool SupportsRollback);
