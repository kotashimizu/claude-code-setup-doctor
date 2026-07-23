using SetupDoctor.Core.Diagnostics;
using SetupDoctor.Core.Models;

namespace SetupDoctor.Core.Policies;

// docs/05 §5.3 の全体判定アルゴリズムを実装する
public sealed class ReadinessAggregator
{
    private static readonly IReadOnlySet<string> SystemCheckIds = new HashSet<string>
    {
        "CHK-OS-001",
        "CHK-ARCH-001",
        "CHK-MEM-001",
    };

    public OverallReadiness Aggregate(IReadOnlyList<DiagnosticResult> results)
    {
        if (results is null || results.Count == 0)
            return OverallReadiness.Unknown;

        var required = results
            .Where(r => !IsNetworkOrOptional(r.CheckId))
            .ToList();

        // システムチェック（OS/Arch/Mem）のいずれかがFailならUnsupported
        var systemFail = results
            .Where(r => SystemCheckIds.Contains(r.CheckId))
            .Any(r => r.Status == DiagnosticStatus.Fail);
        if (systemFail)
            return OverallReadiness.Unsupported;

        // ITAction が必須能力を阻害している場合
        var hasITActionOnRequired = required
            .Any(r => r.Status == DiagnosticStatus.ITAction && IsRequiredCheck(r, results));
        if (hasITActionOnRequired)
            return OverallReadiness.ITActionRequired;

        // 必須チェックにRepairable
        var hasRequiredRepairable = required
            .Any(r => IsRequired(r.CheckId, results) && r.Status == DiagnosticStatus.Repairable);
        if (hasRequiredRepairable)
            return OverallReadiness.Repairable;

        // 必須チェックにUserAction
        var hasUserAction = required
            .Any(r => IsRequired(r.CheckId, results) && r.Status == DiagnosticStatus.UserAction);
        if (hasUserAction)
            return OverallReadiness.UserActionRequired;

        // 必須チェックにFail or Unknown
        var hasRequiredFailOrUnknown = required
            .Any(r => IsRequired(r.CheckId, results)
                      && (r.Status == DiagnosticStatus.Fail || r.Status == DiagnosticStatus.Unknown));
        if (hasRequiredFailOrUnknown)
            return OverallReadiness.Unknown;

        // 全必須Pass、推奨にWarning
        var hasWarning = results.Any(r => r.Status == DiagnosticStatus.Warning);
        if (hasWarning)
            return OverallReadiness.ReadyWithRecommendations;

        return OverallReadiness.Ready;
    }

    // CHK-SHELL-001は「Required conditional」: 他のシェル(PowerShell)が存在しない場合のみRequired
    private static bool IsRequired(string checkId, IReadOnlyList<DiagnosticResult> allResults)
    {
        if (checkId == "CHK-GIT-001")
        {
            // Gitは推奨。PowerShellが利用可能なら必須にしない（Q-001暫定判断）
            var powershellPass = allResults.Any(r =>
                r.CheckId == "CHK-SHELL-001" && r.Status == DiagnosticStatus.Pass);
            return !powershellPass;
        }

        return IsRequiredById(checkId);
    }

    private static bool IsRequired(string checkId) => IsRequiredById(checkId);

    private static bool IsRequiredById(string checkId) => checkId switch
    {
        "CHK-OS-001" => true,
        "CHK-ARCH-001" => true,
        "CHK-MEM-001" => true,
        "CHK-SHELL-001" => true,
        "CHK-CLAUDE-001" => true,
        "CHK-CLAUDE-002" => true,
        "CHK-CLAUDE-003" => true,  // Required when conflict detected (Q-002: CHK側を優先)
        "CHK-PATH-001" => true,
        "CHK-AUTH-001" => true,
        _ => false,
    };

    private static bool IsRequiredCheck(DiagnosticResult result, IReadOnlyList<DiagnosticResult> allResults)
        => IsRequired(result.CheckId, allResults);

    private static bool IsNetworkOrOptional(string checkId) => checkId switch
    {
        "CHK-NET-001" or "CHK-NET-002" or "CHK-NET-003" => true,
        "CHK-DOCTOR-001" => true,
        "CHK-WINGET-001" => true,
        "CHK-ENV-001" => true,
        _ => false,
    };
}
