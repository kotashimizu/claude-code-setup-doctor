using SetupDoctor.Core.Models;

namespace SetupDoctor.Core.Abstractions;

public interface IReportWriter
{
    Task WriteJsonAsync(DiagnosticSession session, string filePath, CancellationToken cancellationToken);
    Task WriteTextAsync(DiagnosticSession session, string filePath, CancellationToken cancellationToken);
}
