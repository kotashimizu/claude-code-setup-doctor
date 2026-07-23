using System.Collections.ObjectModel;
using System.Windows;
using SetupDoctor.Core.Diagnostics;
using SetupDoctor.Core.Models;

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
    private readonly IDiagnosticOrchestrator _orchestrator;

    private AppScreen _currentScreen = AppScreen.Start;
    private bool _isBusy;
    private string _busyMessage = string.Empty;
    private OverallReadiness _readiness;
    private string _overallLabel = string.Empty;
    private CancellationTokenSource? _cts;

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

    public string OverallLabel
    {
        get => _overallLabel;
        private set => SetField(ref _overallLabel, value);
    }

    public ObservableCollection<DiagnosticResultItemViewModel> Results { get; } = [];

    public RelayCommand StartScanCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand ShowReportCommand { get; }

    public MainViewModel(IDiagnosticOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
        StartScanCommand = new RelayCommand(StartScan, () => !IsBusy);
        CancelCommand = new RelayCommand(Cancel, () => IsBusy);
        ShowReportCommand = new RelayCommand(() => CurrentScreen = AppScreen.Completion);
    }

    private void StartScan()
    {
        Results.Clear();
        Readiness = null;
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
                    Readiness = session.OverallReadiness;
                    OverallLabel = session.OverallReadiness switch
                    {
                        OverallReadiness.Ready => "準備完了",
                        OverallReadiness.ReadyWithRecommendations => "準備完了（推奨事項あり）",
                        OverallReadiness.Repairable => "修復可能な問題があります",
                        OverallReadiness.UserActionRequired => "ユーザー操作が必要です",
                        OverallReadiness.ITActionRequired => "IT管理者による対応が必要です",
                        OverallReadiness.Unsupported => "サポート外の環境です",
                        _ => "確認できませんでした",
                    };
                    CurrentScreen = AppScreen.Results;
                });
            }
            catch (OperationCanceledException)
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    BusyMessage = "診断をキャンセルしました。";
                    CurrentScreen = AppScreen.Results;
                });
            }
            catch (Exception ex)
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    BusyMessage = $"エラーが発生しました: {ex.Message}";
                    CurrentScreen = AppScreen.Results;
                });
            }
            finally
            {
                Application.Current?.Dispatcher.Invoke(() => IsBusy = false);
            }
        }, _cts.Token);
    }

    private void Cancel()
    {
        _cts?.Cancel();
        BusyMessage = "キャンセル中…";
    }
}
