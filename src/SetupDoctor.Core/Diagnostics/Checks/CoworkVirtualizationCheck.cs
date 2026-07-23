using SetupDoctor.Core.Abstractions;
using SetupDoctor.Core.Models;

namespace SetupDoctor.Core.Diagnostics.Checks;

// CHK-COWORK-002: Virtual Machine Platform の有効化状態と Windows Edition の整合性。
// Anthropic公式readiness checkerがWindows 11をWindows 10と誤判定する既知バグがあるため、
// ビルド番号ベースで独自に判定する(docs/05 §5.2.1参照)。読み取り専用。
public sealed class CoworkVirtualizationCheck : IDiagnosticCheck
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(8);
    private const int Windows11MinBuild = 22000;

    private readonly ICommandRunner _runner;
    private readonly ISystemInfoProvider _sys;
    private readonly IClock _clock;

    public string Id => "CHK-COWORK-002";

    public CoworkVirtualizationCheck(ICommandRunner runner, ISystemInfoProvider sys, IClock clock)
    {
        _runner = runner;
        _sys = sys;
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
                        "$edition = (Get-ItemProperty 'HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion' " +
                        "-ErrorAction SilentlyContinue).EditionID; " +
                        "$vmp = (Get-WindowsOptionalFeature -Online -FeatureName VirtualMachinePlatform " +
                        "-ErrorAction SilentlyContinue).State; " +
                        "\"$edition|$vmp\""],
                    @"C:\", Timeout),
                cancellationToken);

            if (result.TimedOut)
                return Mk(DiagnosticStatus.Unknown, "COWORK_VIRT_TIMEOUT", "COWORK_PS_TIMEOUT", start);

            var parts = result.StandardOutput.Trim().Split('|');
            var editionId = parts.Length > 0 ? parts[0] : string.Empty;
            var vmpState = parts.Length > 1 ? parts[1] : string.Empty;

            // Windows Home EditionのレジストリEditionIDは歴史的経緯で"Core"("CoreN"はHome N)。
            // "Home"という文字列は実際には出現しないため、正しい値で判定する。
            var isHomeEdition = editionId.StartsWith("Core", StringComparison.OrdinalIgnoreCase);
            var buildStr = _sys.OsBuild;
            var isWindows11 = int.TryParse(buildStr, out var build) && build >= Windows11MinBuild;

            var meta = new Dictionary<string, string>
            {
                ["editionId"] = string.IsNullOrEmpty(editionId) ? "unknown" : editionId,
                ["osBuild"] = buildStr ?? "unknown",
                ["vmpState"] = string.IsNullOrEmpty(vmpState) ? "unknown" : vmpState,
            };

            if (string.IsNullOrEmpty(vmpState))
                return Mk(DiagnosticStatus.Unknown, "COWORK_VIRT_CHECK_INCONCLUSIVE", "COWORK_VMP_STATE_UNKNOWN",
                    start, meta);

            if (vmpState.Equals("Enabled", StringComparison.OrdinalIgnoreCase))
            {
                // Home Editionは有効でも動作が不安定という矛盾する報告が複数あるため楽観視しない
                if (isHomeEdition)
                    return Mk(DiagnosticStatus.Unknown, "COWORK_VIRT_HOME_EDITION_UNCERTAIN",
                        "COWORK_HOME_EDITION_MAY_STILL_FAIL", start, meta);

                return Mk(DiagnosticStatus.Pass, "COWORK_VIRT_ENABLED", "COWORK_VMP_OK", start, meta);
            }

            if (vmpState.Equals("Disabled", StringComparison.OrdinalIgnoreCase))
            {
                if (!isWindows11 && !string.IsNullOrEmpty(buildStr))
                    meta["note"] = "windows10-or-older";

                return Mk(DiagnosticStatus.Repairable, "COWORK_VIRT_DISABLED", "COWORK_VMP_DISABLED", start,
                    new Dictionary<string, string>(meta) { ["remediationId"] = "REM-COWORK-ENABLE-VMP" });
            }

            return Mk(DiagnosticStatus.Unknown, "COWORK_VIRT_UNKNOWN_STATE", $"COWORK_VMP_{vmpState}", start, meta);
        }
        catch
        {
            return Mk(DiagnosticStatus.Unknown, "COWORK_VIRT_CHECK_FAILED", "COWORK_PS_UNAVAILABLE", start);
        }
    }

    private DiagnosticResult Mk(DiagnosticStatus status, string summary, string detail,
        DateTimeOffset start, Dictionary<string, string>? meta = null)
        => new(Id, status, summary, detail,
            meta ?? new Dictionary<string, string>(),
            _clock.UtcNow - start, _clock.UtcNow);
}
