using SetupDoctor.Core.Abstractions;
using SetupDoctor.Core.Models;

namespace SetupDoctor.Core.Diagnostics.Checks;

public sealed class PowerShell7Check : IDiagnosticCheck
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly ICommandRunner _runner;
    private readonly IClock _clock;

    public string Id => "CHK-SHELL-002";

    public PowerShell7Check(ICommandRunner runner, IClock clock)
    {
        _runner = runner;
        _clock = clock;
    }

    public async Task<DiagnosticResult> RunAsync(DiagnosticContext context, CancellationToken cancellationToken)
    {
        var start = _clock.UtcNow;

        var req = new CommandRequest(
            "pwsh.exe",
            ["--version"],
            WorkingDirectory: @"C:\",
            Timeout: Timeout);

        try
        {
            var result = await _runner.RunAsync(req, cancellationToken);

            if (result.TimedOut || result.ExitCode != 0)
                return Mk(DiagnosticStatus.Warning, "SHELL_PS7_NOT_FOUND", "SHELL_PS7_ABSENT", start);

            var version = result.StandardOutput.Trim();
            return Mk(DiagnosticStatus.Pass, "SHELL_PS7_AVAILABLE", "SHELL_PS7_OK", start,
                new Dictionary<string, string> { ["ps7Version"] = version });
        }
        catch
        {
            return Mk(DiagnosticStatus.Warning, "SHELL_PS7_NOT_FOUND", "SHELL_PS7_ABSENT", start);
        }
    }

    private DiagnosticResult Mk(DiagnosticStatus status, string summary, string detail,
        DateTimeOffset start, Dictionary<string, string>? meta = null)
        => new(Id, status, summary, detail,
            meta ?? new Dictionary<string, string>(),
            _clock.UtcNow - start, _clock.UtcNow);
}
