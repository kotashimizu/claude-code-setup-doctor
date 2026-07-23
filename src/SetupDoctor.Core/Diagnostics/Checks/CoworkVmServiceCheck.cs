using SetupDoctor.Core.Abstractions;
using SetupDoctor.Core.Models;

namespace SetupDoctor.Core.Diagnostics.Checks;

// CHK-COWORK-001: Claude Desktop の Cowork 用仮想マシンサービス(CoworkVMService)の稼働状態。
// サービス名は非公式情報(docs/05 §5.2.1参照)。読み取り専用、状態変更はしない。
public sealed class CoworkVmServiceCheck : IDiagnosticCheck
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
    private const string ServiceName = "CoworkVMService";

    private readonly ICommandRunner _runner;
    private readonly IClock _clock;

    public string Id => "CHK-COWORK-001";

    public CoworkVmServiceCheck(ICommandRunner runner, IClock clock)
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
                new CommandRequest("powershell.exe",
                    ["-NoProfile", "-NonInteractive", "-Command",
                        $"$s = Get-Service -Name '{ServiceName}' -ErrorAction SilentlyContinue; " +
                        "if ($null -eq $s) { 'NOTFOUND' } else { $s.Status }"],
                    @"C:\", Timeout),
                cancellationToken);

            if (result.TimedOut)
                return Mk(DiagnosticStatus.Unknown, "COWORK_SERVICE_TIMEOUT", "COWORK_PS_TIMEOUT", start);

            var status = result.StandardOutput.Trim();

            if (status.Equals("NOTFOUND", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(status))
                return Mk(DiagnosticStatus.Unknown, "COWORK_SERVICE_NOT_FOUND", "COWORK_VM_SERVICE_ABSENT", start);

            if (status.Equals("Running", StringComparison.OrdinalIgnoreCase))
                return Mk(DiagnosticStatus.Pass, "COWORK_SERVICE_RUNNING", "COWORK_VM_SERVICE_OK", start);

            if (status.Equals("Stopped", StringComparison.OrdinalIgnoreCase))
                return Mk(DiagnosticStatus.Repairable, "COWORK_SERVICE_STOPPED", "COWORK_VM_SERVICE_STOPPED", start,
                    new Dictionary<string, string> { ["remediationId"] = "REM-COWORK-START-SERVICE" });

            return Mk(DiagnosticStatus.Unknown, "COWORK_SERVICE_UNKNOWN_STATE", $"COWORK_STATUS_{status}", start);
        }
        catch
        {
            return Mk(DiagnosticStatus.Unknown, "COWORK_SERVICE_CHECK_FAILED", "COWORK_PS_UNAVAILABLE", start);
        }
    }

    private DiagnosticResult Mk(DiagnosticStatus status, string summary, string detail,
        DateTimeOffset start, Dictionary<string, string>? meta = null)
        => new(Id, status, summary, detail,
            meta ?? new Dictionary<string, string>(),
            _clock.UtcNow - start, _clock.UtcNow);
}
