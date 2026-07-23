namespace SetupDoctor.Core.Diagnostics;

public interface IDiagnosticCheck
{
    string Id { get; }
    Task<DiagnosticResult> RunAsync(DiagnosticContext context, CancellationToken cancellationToken);
}
