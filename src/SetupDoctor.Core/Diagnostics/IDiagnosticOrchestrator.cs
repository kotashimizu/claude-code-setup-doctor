using SetupDoctor.Core.Models;

namespace SetupDoctor.Core.Diagnostics;

public interface IDiagnosticOrchestrator
{
    Task<DiagnosticSession> RunBasicDiagnosticsAsync(
        IProgress<DiagnosticResult>? progress,
        CancellationToken cancellationToken);
}
