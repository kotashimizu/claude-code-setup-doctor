using SetupDoctor.Core.Abstractions;

namespace SetupDoctor.Core.Diagnostics.Checks;

// CHK-COWORK-006: 組織(Team/Enterprise)によるCowork無効化の間接シグナル。
// ローカルのWindows診断からは組織設定を直接確認する手段がないため、常にUnknown(Informational)として
// 「他のCowork項目が全てPassなのに機能しない場合はここを疑ってください」という案内のみ表示する。
public sealed class CoworkOrgPolicyHintCheck : IDiagnosticCheck
{
    private readonly IClock _clock;

    public string Id => "CHK-COWORK-006";

    public CoworkOrgPolicyHintCheck(IClock clock)
    {
        _clock = clock;
    }

    public Task<DiagnosticResult> RunAsync(DiagnosticContext context, CancellationToken cancellationToken)
    {
        var start = _clock.UtcNow;

        // 他のCowork項目が全てPass/NotApplicableかどうかで、この案内を出す価値があるかを判断する
        var otherCoworkChecks = new[] { "CHK-COWORK-001", "CHK-COWORK-002", "CHK-COWORK-003",
            "CHK-COWORK-004", "CHK-COWORK-005" };

        var allHealthy = otherCoworkChecks
            .Select(context.GetPrior)
            .Where(r => r is not null)
            .All(r => r!.Status is DiagnosticStatus.Pass or DiagnosticStatus.NotApplicable);

        if (!allHealthy)
            return Task.FromResult(Mk(DiagnosticStatus.NotApplicable, "COWORK_ORG_HINT_SKIPPED",
                "COWORK_OTHER_ISSUES_FOUND_FIRST", start));

        return Task.FromResult(Mk(DiagnosticStatus.Unknown, "COWORK_ORG_HINT",
            "COWORK_CHECK_ORG_CAPABILITIES_SETTING", start));
    }

    private DiagnosticResult Mk(DiagnosticStatus status, string summary, string detail,
        DateTimeOffset start, Dictionary<string, string>? meta = null)
        => new(Id, status, summary, detail,
            meta ?? new Dictionary<string, string>(),
            _clock.UtcNow - start, _clock.UtcNow);
}
