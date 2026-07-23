using SetupDoctor.Core.Abstractions;
using SetupDoctor.Core.Models;

namespace SetupDoctor.Core.Diagnostics.Checks;

// CHK-WINGET-001: WinGetの利用可否（Optional）
public sealed class WinGetCheck : IDiagnosticCheck
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly ICommandRunner _runner;
    private readonly IClock _clock;

    public string Id => "CHK-WINGET-001";

    public WinGetCheck(ICommandRunner runner, IClock clock)
    {
        _runner = runner;
        _clock = clock;
    }

    public async Task<DiagnosticResult> RunAsync(DiagnosticContext context, CancellationToken cancellationToken)
    {
        var start = _clock.UtcNow;

        try
        {
            var result = await _runner.RunAsync(
                new CommandRequest("winget.exe", ["--version"], @"C:\", Timeout),
                cancellationToken);

            if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.StandardOutput))
            {
                return Mk(DiagnosticStatus.Pass, "WINGET_AVAILABLE", "WINGET_OK", start,
                    new Dictionary<string, string> { ["wingetVersion"] = result.StandardOutput.Trim() });
            }
        }
        catch { /* winget不在 */ }

        return Mk(DiagnosticStatus.Warning, "WINGET_NOT_FOUND", "WINGET_ABSENT", start);
    }

    private DiagnosticResult Mk(DiagnosticStatus status, string summary, string detail,
        DateTimeOffset start, Dictionary<string, string>? meta = null)
        => new(Id, status, summary, detail,
            meta ?? new Dictionary<string, string>(),
            _clock.UtcNow - start, _clock.UtcNow);
}
