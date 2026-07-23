using SetupDoctor.Core.Diagnostics;

namespace SetupDoctor.App.ViewModels;

public sealed class DiagnosticResultItemViewModel : ViewModelBase
{
    public string CheckId { get; }
    public DiagnosticStatus Status { get; }
    public string SummaryKey { get; }
    public string DetailCode { get; }
    public TimeSpan Duration { get; }

    // UIラベル（日本語リソースキーはStatus→ラベル変換で引く）
    public string StatusLabel => Status switch
    {
        DiagnosticStatus.Pass => "合格",
        DiagnosticStatus.Fail => "失敗",
        DiagnosticStatus.Warning => "警告",
        DiagnosticStatus.Repairable => "修復可能",
        DiagnosticStatus.UserAction => "操作が必要",
        DiagnosticStatus.ITAction => "IT管理者が対応",
        DiagnosticStatus.NotApplicable => "非対象",
        _ => "不明",
    };

    public bool CanRepair => Status == DiagnosticStatus.Repairable;
    public bool IsPass => Status == DiagnosticStatus.Pass;
    public bool IsFail => Status is DiagnosticStatus.Fail or DiagnosticStatus.Warning
        or DiagnosticStatus.Repairable or DiagnosticStatus.UserAction or DiagnosticStatus.ITAction;

    public DiagnosticResultItemViewModel(DiagnosticResult result)
    {
        CheckId = result.CheckId;
        Status = result.Status;
        SummaryKey = result.SummaryKey;
        DetailCode = result.DetailCode;
        Duration = result.Duration;
    }
}
