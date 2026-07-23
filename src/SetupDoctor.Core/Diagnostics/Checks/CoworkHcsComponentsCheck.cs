using SetupDoctor.Core.Abstractions;
using SetupDoctor.Core.Models;

namespace SetupDoctor.Core.Diagnostics.Checks;

// CHK-COWORK-003: HCS関連コンポーネント(hns / vmcompute / vfpext)の登録状態。
// 読み取り専用。Anthropic公式ヘルプの "Missing HCS services: HNS, vmcompute, vfpext" エラー文言に対応。
public sealed class CoworkHcsComponentsCheck : IDiagnosticCheck
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(8);

    private readonly ICommandRunner _runner;
    private readonly IClock _clock;

    public string Id => "CHK-COWORK-003";

    public CoworkHcsComponentsCheck(ICommandRunner runner, IClock clock)
    {
        _runner = runner;
        _clock = clock;
    }

    public async Task<DiagnosticResult> RunAsync(DiagnosticContext context, CancellationToken cancellationToken)
    {
        var start = _clock.UtcNow;

        // 前提: CHK-COWORK-002でVMP自体が無効/不明ならこのチェックは無意味なのでスキップ
        var virtResult = context.GetPrior("CHK-COWORK-002");
        if (virtResult is not null && virtResult.Status is not (DiagnosticStatus.Pass or DiagnosticStatus.Repairable))
            return Mk(DiagnosticStatus.NotApplicable, "COWORK_HCS_SKIPPED", "COWORK_VMP_NOT_READY", start);

        try
        {
            var result = await _runner.RunAsync(
                new CommandRequest("powershell.exe",
                    ["-NoProfile", "-NonInteractive", "-Command",
                        "$hns = $null -ne (Get-Service -Name hns -ErrorAction SilentlyContinue); " +
                        "$vmcompute = $null -ne (Get-Service -Name vmcompute -ErrorAction SilentlyContinue); " +
                        "\"$hns|$vmcompute\""],
                    @"C:\", Timeout),
                cancellationToken);

            if (result.TimedOut)
                return Mk(DiagnosticStatus.Unknown, "COWORK_HCS_TIMEOUT", "COWORK_PS_TIMEOUT", start);

            var parts = result.StandardOutput.Trim().Split('|');
            var hnsPresent = parts.Length > 0 && parts[0].Equals("True", StringComparison.OrdinalIgnoreCase);
            var vmcomputePresent = parts.Length > 1 && parts[1].Equals("True", StringComparison.OrdinalIgnoreCase);

            // vfpextはドライバのため sc query で別途確認(ベストエフォート)
            var vfpextPresent = await CheckDriverAsync("vfpext", cancellationToken);

            var meta = new Dictionary<string, string>
            {
                ["hns"] = hnsPresent.ToString(),
                ["vmcompute"] = vmcomputePresent.ToString(),
                ["vfpext"] = vfpextPresent.ToString(),
            };

            if (hnsPresent && vmcomputePresent && vfpextPresent)
                return Mk(DiagnosticStatus.Pass, "COWORK_HCS_OK", "COWORK_HCS_ALL_PRESENT", start, meta);

            return Mk(DiagnosticStatus.Repairable, "COWORK_HCS_MISSING", "COWORK_HCS_COMPONENT_ABSENT", start,
                new Dictionary<string, string>(meta) { ["remediationId"] = "REM-COWORK-REPAIR-HCS" });
        }
        catch
        {
            return Mk(DiagnosticStatus.Unknown, "COWORK_HCS_CHECK_FAILED", "COWORK_PS_UNAVAILABLE", start);
        }
    }

    private async Task<bool> CheckDriverAsync(string driverName, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _runner.RunAsync(
                new CommandRequest("sc.exe", ["query", driverName], @"C:\", TimeSpan.FromSeconds(5)),
                cancellationToken);
            return result.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private DiagnosticResult Mk(DiagnosticStatus status, string summary, string detail,
        DateTimeOffset start, Dictionary<string, string>? meta = null)
        => new(Id, status, summary, detail,
            meta ?? new Dictionary<string, string>(),
            _clock.UtcNow - start, _clock.UtcNow);
}
