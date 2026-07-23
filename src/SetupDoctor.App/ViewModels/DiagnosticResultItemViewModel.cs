using System.Windows;
using SetupDoctor.Core.Diagnostics;

namespace SetupDoctor.App.ViewModels;

public sealed class DiagnosticResultItemViewModel : ViewModelBase
{
    public string CheckId { get; }
    public DiagnosticStatus Status { get; }
    public string SummaryKey { get; }
    public string DetailCode { get; }
    public TimeSpan Duration { get; }

    // チェックIDの日本語表示名（Strings.ja.xaml の Check_<CheckId> を参照。未登録ならIDをそのまま表示）
    public string DisplayName =>
        Application.Current?.TryFindResource($"Check_{CheckId}") as string ?? CheckId;

    // docs/03_ui_ux_spec.md 3.5 の状態語（厳密一致）。Strings.ja.xaml の Status_<Status> を参照
    public string StatusLabel =>
        Application.Current?.TryFindResource($"Status_{Status}") as string ?? Status.ToString();

    public string RequirementLevelLabel =>
        Application.Current?.TryFindResource($"Level_{CheckExplanations.RequirementLevelKey(CheckId)}") as string
        ?? string.Empty;

    // ITが苦手な人にも伝わる一言説明（docs/03 3.4 S-03 の例に準拠）
    public string FriendlySummary => CheckExplanations.Explain(CheckId, Status);

    public bool CanRepair => Status == DiagnosticStatus.Repairable;
    public bool IsPass => Status == DiagnosticStatus.Pass;

    // 「対応不要」とみなす状態（サマリー行にまとめ、個別強調しない）
    public bool IsNeutral => Status is DiagnosticStatus.Pass or DiagnosticStatus.NotApplicable;

    // 個別に目立たせて表示すべき状態
    public bool IsActionable => !IsNeutral;

    public bool IsFail => Status is DiagnosticStatus.Fail or DiagnosticStatus.Warning
        or DiagnosticStatus.Repairable or DiagnosticStatus.UserAction or DiagnosticStatus.ITAction;

    public int SeverityRank => CheckExplanations.Severity(Status);

    public DiagnosticResultItemViewModel(DiagnosticResult result)
    {
        CheckId = result.CheckId;
        Status = result.Status;
        SummaryKey = result.SummaryKey;
        DetailCode = result.DetailCode;
        Duration = result.Duration;
    }
}
