using SetupDoctor.Core.Abstractions;
using SetupDoctor.Core.Models;

namespace SetupDoctor.Core.Diagnostics.Checks;

public sealed class WindowsPowerShellCheck : IDiagnosticCheck
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly ICommandRunner _runner;
    private readonly IClock _clock;

    public string Id => "CHK-SHELL-001";

    public WindowsPowerShellCheck(ICommandRunner runner, IClock clock)
    {
        _runner = runner;
        _clock = clock;
    }

    public async Task<DiagnosticResult> RunAsync(DiagnosticContext context, CancellationToken cancellationToken)
    {
        var start = _clock.UtcNow;

        // Windows PowerShell の既定場所を優先して探す
        var candidates = new[]
        {
            @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe",
            "powershell.exe",
        };

        foreach (var exe in candidates)
        {
            var req = new CommandRequest(
                exe,
                ["-NoProfile", "-NonInteractive", "-Command", "$PSVersionTable.PSVersion.ToString()"],
                WorkingDirectory: @"C:\",
                Timeout: Timeout);

            CommandResult result;
            try
            {
                result = await _runner.RunAsync(req, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return Mk(DiagnosticStatus.Unknown, "SHELL_PS_CANCELLED", "SHELL_PS_TIMEOUT", start);
            }

            if (result.TimedOut)
                return Mk(DiagnosticStatus.Unknown, "SHELL_PS_TIMEOUT", "SHELL_PS_TIMEOUT", start);

            if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.StandardOutput))
            {
                var version = result.StandardOutput.Trim();
                return Mk(DiagnosticStatus.Pass, "SHELL_PS_AVAILABLE", "SHELL_PS_OK", start,
                    new Dictionary<string, string> { ["psVersion"] = version, ["executableMasked"] = "[masked]" });
            }

            // exit 0以外はポリシーブロックの可能性
            if (result.ExitCode == 5 || result.ExitCode == -196608)  // Access denied / policy blocked
                return Mk(DiagnosticStatus.ITAction, "SHELL_PS_POLICY_BLOCKED", "SHELL_PS_ACCESS_DENIED", start);
        }

        return Mk(DiagnosticStatus.Repairable, "SHELL_PS_NOT_IN_PATH", "SHELL_PS_ADD_TO_PATH", start);
    }

    private DiagnosticResult Mk(DiagnosticStatus status, string summary, string detail,
        DateTimeOffset start, Dictionary<string, string>? meta = null)
        => new(Id, status, summary, detail,
            meta ?? new Dictionary<string, string>(),
            _clock.UtcNow - start, _clock.UtcNow);
}
