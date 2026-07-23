namespace SetupDoctor.Core.Remediation;

public sealed record RemediationPreview(
    RemediationPlanItem Plan,
    bool IsFeasible,
    string? FeasibilityNote);
