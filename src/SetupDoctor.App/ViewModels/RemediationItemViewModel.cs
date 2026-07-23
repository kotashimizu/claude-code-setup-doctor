using SetupDoctor.Core.Remediation;

namespace SetupDoctor.App.ViewModels;

public sealed class RemediationItemViewModel : ViewModelBase
{
    private bool _isSelected;
    private string _statusText = "未実行";

    public RemediationPlanItem Plan { get; }
    public bool IsFeasible { get; }
    public string? FeasibilityNote { get; }

    public string Title => Plan.Title;
    public string Target => Plan.Target;
    public string BeforeSummary => Plan.BeforeSummary;
    public string AfterSummary => Plan.AfterSummary;
    public bool RequiresElevation => Plan.RequiresElevation;
    public bool RequiresNewProcess => Plan.RequiresNewProcess;
    public string RequiresNewProcessLabel => Plan.RequiresNewProcess ? "必要" : "不要";

    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetField(ref _statusText, value);
    }

    public RemediationItemViewModel(RemediationPreview preview)
    {
        Plan = preview.Plan;
        IsFeasible = preview.IsFeasible;
        FeasibilityNote = preview.FeasibilityNote;
        // 実行不可能な項目は誤って実行されないよう、既定で未選択にする
        _isSelected = preview.IsFeasible;
        if (!preview.IsFeasible)
            StatusText = "自動修復できません";
    }

    public void ApplyResult(RemediationExecutionResult result)
    {
        StatusText = result.Status switch
        {
            RemediationStatus.Verified => "完了",
            RemediationStatus.Failed => $"失敗: {result.ErrorMessage}",
            RemediationStatus.Cancelled => "キャンセルされました",
            _ => result.Status.ToString(),
        };
    }
}
