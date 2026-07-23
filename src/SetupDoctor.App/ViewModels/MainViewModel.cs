using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using SetupDoctor.Core.Abstractions;
using SetupDoctor.Core.Diagnostics;
using SetupDoctor.Core.Models;
using SetupDoctor.Core.Remediation;

namespace SetupDoctor.App.ViewModels;

public enum AppScreen
{
    Start,
    Scanning,
    Results,
    RemediationReview,
    RemediationExecuting,
    Completion,
}

public sealed class MainViewModel : ViewModelBase
{
    // CheckId -> 実装済みRemediationId。両方のCheckIdが同じ修復を指す場合は重複排除する
    private static readonly IReadOnlyDictionary<string, string> CheckToRemediationId =
        new Dictionary<string, string>
        {
            ["CHK-PATH-001"] = "REM-PATH-CLAUDE-LOCALBIN",
            ["CHK-CLAUDE-001"] = "REM-PATH-CLAUDE-LOCALBIN",
            ["CHK-GITBASH-001"] = "REM-CLAUDE-GITBASH-SETTING",
        };

    private readonly IDiagnosticOrchestrator _orchestrator;
    private readonly IRemediationOrchestrator _remediationOrchestrator;
    private readonly IReadOnlyDictionary<string, IRemediationAction> _remediationActions;
    private readonly IReportWriter _reportWriter;

    private AppScreen _currentScreen = AppScreen.Start;
    private bool _isBusy;
    private string _busyMessage = string.Empty;
    private OverallReadiness _readiness;
    private string _overallHeadline = string.Empty;
    private string _overallSub = string.Empty;
    private string _passSummaryText = string.Empty;
    private bool _showTechnicalDetail;
    private string? _lastReportPath;
    private bool _remediationConfirmed;
    private bool _hasUnsupportedRemediation;
    private string _completionMessage = string.Empty;
    private DiagnosticSession? _lastSession;
    private CancellationTokenSource? _cts;
    private CancellationTokenSource? _remediationCts;

    public AppScreen CurrentScreen
    {
        get => _currentScreen;
        private set => SetField(ref _currentScreen, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            SetField(ref _isBusy, value);
            StartScanCommand.RaiseCanExecuteChanged();
            CancelCommand.RaiseCanExecuteChanged();
        }
    }

    public string BusyMessage
    {
        get => _busyMessage;
        private set => SetField(ref _busyMessage, value);
    }

    public OverallReadiness Readiness
    {
        get => _readiness;
        private set => SetField(ref _readiness, value);
    }

    // docs/03 3.4 S-03 の「見出し」
    public string OverallHeadline
    {
        get => _overallHeadline;
        private set => SetField(ref _overallHeadline, value);
    }

    // docs/03 3.4 S-03 の「補足」
    public string OverallSub
    {
        get => _overallSub;
        private set => SetField(ref _overallSub, value);
    }

    // 例:「18項目中15項目は問題ありません」
    public string PassSummaryText
    {
        get => _passSummaryText;
        private set => SetField(ref _passSummaryText, value);
    }

    public bool ShowTechnicalDetail
    {
        get => _showTechnicalDetail;
        set => SetField(ref _showTechnicalDetail, value);
    }

    public string? LastReportPath
    {
        get => _lastReportPath;
        private set
        {
            SetField(ref _lastReportPath, value);
            OpenReportFolderCommand.RaiseCanExecuteChanged();
        }
    }

    public bool RemediationConfirmed
    {
        get => _remediationConfirmed;
        set
        {
            SetField(ref _remediationConfirmed, value);
            ExecuteRemediationCommand.RaiseCanExecuteChanged();
        }
    }

    // 選択された修復の中に自動修復が未対応の項目が含まれるか
    public bool HasUnsupportedRemediation
    {
        get => _hasUnsupportedRemediation;
        private set => SetField(ref _hasUnsupportedRemediation, value);
    }

    public string CompletionMessage
    {
        get => _completionMessage;
        private set => SetField(ref _completionMessage, value);
    }

    private bool _hasRepairableItems;

    // 「自動で直す」ボタンの表示可否
    public bool HasRepairableItems
    {
        get => _hasRepairableItems;
        private set => SetField(ref _hasRepairableItems, value);
    }

    // 全項目（技術詳細テーブル用）
    public ObservableCollection<DiagnosticResultItemViewModel> Results { get; } = [];

    // 対応が必要な項目だけ、重要度順（結果画面のメイン表示用）
    public ObservableCollection<DiagnosticResultItemViewModel> ActionableResults { get; } = [];

    // 自動修復の対象一覧（レビュー・実行画面で共有）
    public ObservableCollection<RemediationItemViewModel> RemediationPlan { get; } = [];

    public RelayCommand StartScanCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand SaveReportCommand { get; }
    public RelayCommand OpenReportFolderCommand { get; }
    public RelayCommand GoToRemediationReviewCommand { get; }
    public RelayCommand BackToResultsCommand { get; }
    public RelayCommand ExecuteRemediationCommand { get; }
    public RelayCommand CancelRemediationCommand { get; }
    public RelayCommand BackToStartCommand { get; }

    public MainViewModel(
        IDiagnosticOrchestrator orchestrator,
        IRemediationOrchestrator remediationOrchestrator,
        IReadOnlyDictionary<string, IRemediationAction> remediationActions,
        IReportWriter reportWriter)
    {
        _orchestrator = orchestrator;
        _remediationOrchestrator = remediationOrchestrator;
        _remediationActions = remediationActions;
        _reportWriter = reportWriter;

        StartScanCommand = new RelayCommand(StartScan, () => !IsBusy);
        CancelCommand = new RelayCommand(Cancel, () => IsBusy);
        SaveReportCommand = new RelayCommand(async () => await SaveReportAsync());
        OpenReportFolderCommand = new RelayCommand(OpenReportFolder, () => LastReportPath is not null);
        GoToRemediationReviewCommand = new RelayCommand(GoToRemediationReview,
            () => ActionableResults.Any(r => r.CanRepair));
        BackToResultsCommand = new RelayCommand(() => CurrentScreen = AppScreen.Results);
        ExecuteRemediationCommand = new RelayCommand(
            async () => await ExecuteRemediationAsync(),
            () => RemediationConfirmed && RemediationPlan.Any(r => r.IsSelected));
        CancelRemediationCommand = new RelayCommand(() => _remediationCts?.Cancel());
        BackToStartCommand = new RelayCommand(() =>
        {
            Results.Clear();
            ActionableResults.Clear();
            RemediationPlan.Clear();
            LastReportPath = null;
            CurrentScreen = AppScreen.Start;
        });
    }

    private void StartScan()
    {
        Results.Clear();
        ActionableResults.Clear();
        LastReportPath = null;
        Readiness = OverallReadiness.Unknown;
        IsBusy = true;
        BusyMessage = "診断を実行中です…";
        CurrentScreen = AppScreen.Scanning;
        _cts = new CancellationTokenSource();

        Task.Run(async () =>
        {
            try
            {
                var progress = new Progress<DiagnosticResult>(r =>
                    Application.Current?.Dispatcher.Invoke(() =>
                        Results.Add(new DiagnosticResultItemViewModel(r))));

                var session = await _orchestrator.RunBasicDiagnosticsAsync(progress, _cts.Token);

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    _lastSession = session;
                    ApplyReadiness(session);
                    CurrentScreen = AppScreen.Results;
                });
            }
            catch (OperationCanceledException)
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    Readiness = OverallReadiness.Unknown;
                    OverallHeadline = "診断をキャンセルしました";
                    OverallSub = "「もう一度診断」を押すと再実行できます。";
                    PassSummaryText = string.Empty;
                    CurrentScreen = AppScreen.Results;
                });
            }
            catch (Exception ex)
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    Readiness = OverallReadiness.Unknown;
                    OverallHeadline = "エラーが発生しました";
                    OverallSub = $"診断中に問題が発生しました。もう一度お試しください。（詳細: {ex.Message}）";
                    PassSummaryText = string.Empty;
                    CurrentScreen = AppScreen.Results;
                });
            }
            finally
            {
                Application.Current?.Dispatcher.Invoke(() => IsBusy = false);
            }
        }, _cts.Token);
    }

    private void ApplyReadiness(DiagnosticSession session)
    {
        Readiness = session.OverallReadiness;

        var key = session.OverallReadiness.ToString();
        OverallHeadline = Application.Current?.TryFindResource($"Overall_{key}_Headline") as string
            ?? "状態を確認できませんでした";
        OverallSub = Application.Current?.TryFindResource($"Overall_{key}_Sub") as string ?? string.Empty;

        ActionableResults.Clear();
        foreach (var item in Results.Where(r => r.IsActionable).OrderBy(r => r.SeverityRank))
            ActionableResults.Add(item);

        var passCount = Results.Count(r => r.IsNeutral);
        PassSummaryText = $"{Results.Count}項目中{passCount}項目は問題ありません。";

        HasRepairableItems = ActionableResults.Any(r => r.CanRepair);
        GoToRemediationReviewCommand.RaiseCanExecuteChanged();
    }

    private void Cancel()
    {
        _cts?.Cancel();
        BusyMessage = "キャンセル中…";
    }

    private async Task SaveReportAsync()
    {
        if (_lastSession is null) return;

        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "ClaudeCodeSetupDoctor", "Reports");
            Directory.CreateDirectory(dir);

            var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
            var jsonPath = Path.Combine(dir, $"diagnostic-report-{timestamp}.json");
            var textPath = Path.Combine(dir, $"diagnostic-report-{timestamp}.txt");

            await _reportWriter.WriteJsonAsync(_lastSession, jsonPath, CancellationToken.None);
            await _reportWriter.WriteTextAsync(_lastSession, textPath, CancellationToken.None);

            LastReportPath = textPath;
        }
        catch (Exception ex)
        {
            BusyMessage = $"レポートの保存に失敗しました: {ex.Message}";
        }
    }

    private void OpenReportFolder()
    {
        if (LastReportPath is null) return;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                UseShellExecute = false,
            };
            psi.ArgumentList.Add($"/select,{LastReportPath}");
            Process.Start(psi);
        }
        catch { /* エクスプローラーを開けなくても致命的ではない */ }
    }

    private void GoToRemediationReview()
    {
        RemediationPlan.Clear();
        HasUnsupportedRemediation = false;

        var repairableCheckIds = ActionableResults
            .Where(r => r.CanRepair)
            .Select(r => r.CheckId)
            .ToList();

        var remediationIds = new List<string>();
        foreach (var checkId in repairableCheckIds)
        {
            if (CheckToRemediationId.TryGetValue(checkId, out var remId))
            {
                if (!remediationIds.Contains(remId))
                    remediationIds.Add(remId);
            }
            else
            {
                HasUnsupportedRemediation = true;
            }
        }

        Task.Run(async () =>
        {
            var items = new List<RemediationItemViewModel>();
            foreach (var remId in remediationIds)
            {
                if (!_remediationActions.TryGetValue(remId, out var action)) continue;

                var previewContext = new RemediationContext(remId, string.Empty,
                    new Dictionary<string, string>());
                try
                {
                    var preview = await action.PreviewAsync(previewContext, CancellationToken.None);
                    items.Add(new RemediationItemViewModel(preview));
                }
                catch { /* プレビュー失敗はスキップ */ }
            }

            Application.Current?.Dispatcher.Invoke(() =>
            {
                foreach (var item in items)
                {
                    // 選択状態が変わるたびに「実行」ボタンのCanExecuteを再評価する
                    item.PropertyChanged += (_, e) =>
                    {
                        if (e.PropertyName == nameof(RemediationItemViewModel.IsSelected))
                            ExecuteRemediationCommand.RaiseCanExecuteChanged();
                    };
                    RemediationPlan.Add(item);
                }

                RemediationConfirmed = false;
                CurrentScreen = AppScreen.RemediationReview;
                ExecuteRemediationCommand.RaiseCanExecuteChanged();
            });
        });
    }

    private async Task ExecuteRemediationAsync()
    {
        var selected = RemediationPlan.Where(r => r.IsSelected).ToList();
        if (selected.Count == 0) return;

        CurrentScreen = AppScreen.RemediationExecuting;
        IsBusy = true;
        _remediationCts = new CancellationTokenSource();

        foreach (var item in selected)
            item.StatusText = "待機中";

        try
        {
            var plan = selected.Select(s => s.Plan).ToList();
            var progress = new Progress<RemediationExecutionResult>(r =>
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    var target = selected.FirstOrDefault(s => s.Plan.RemediationId == r.RemediationId);
                    target?.ApplyResult(r);
                }));

            await _remediationOrchestrator.ExecuteAsync(plan, progress, _remediationCts.Token);

            // 修復後は必ず再診断してから完了画面へ（未検証のまま完了扱いにしない）
            BusyMessage = "変更後の状態を再確認しています…";
            var rescanProgress = new Progress<DiagnosticResult>(r =>
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    var existing = Results.FirstOrDefault(x => x.CheckId == r.CheckId);
                    if (existing is not null) Results.Remove(existing);
                    Results.Add(new DiagnosticResultItemViewModel(r));
                }));

            var session = await _orchestrator.RunBasicDiagnosticsAsync(rescanProgress, CancellationToken.None);
            _lastSession = session;
            ApplyReadiness(session);

            var neededNewProcess = selected.Any(s => s.RequiresNewProcess);
            CompletionMessage = session.OverallReadiness switch
            {
                OverallReadiness.Ready => Application.Current?.TryFindResource("CompletionSuccess") as string
                    ?? string.Empty,
                OverallReadiness.ReadyWithRecommendations when neededNewProcess =>
                    Application.Current?.TryFindResource("CompletionPartial") as string ?? string.Empty,
                OverallReadiness.ReadyWithRecommendations =>
                    Application.Current?.TryFindResource("CompletionSuccess") as string ?? string.Empty,
                _ when neededNewProcess =>
                    Application.Current?.TryFindResource("CompletionPartial") as string ?? string.Empty,
                _ => Application.Current?.TryFindResource("CompletionFailed") as string ?? string.Empty,
            };

            CurrentScreen = AppScreen.Completion;
        }
        catch (OperationCanceledException)
        {
            CompletionMessage = "修復をキャンセルしました。完了済みの変更は元に戻っていません。";
            CurrentScreen = AppScreen.Completion;
        }
        catch (Exception ex)
        {
            CompletionMessage = $"修復中にエラーが発生しました: {ex.Message}";
            CurrentScreen = AppScreen.Completion;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
