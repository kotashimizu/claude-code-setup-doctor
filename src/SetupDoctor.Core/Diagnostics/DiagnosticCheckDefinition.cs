namespace SetupDoctor.Core.Diagnostics;

public sealed record DiagnosticCheckDefinition(
    string Id,
    string DisplayName,
    RequirementLevel Requirement,
    TimeSpan Timeout,
    bool UsesNetwork,
    IReadOnlyList<string> RemediationIds);
