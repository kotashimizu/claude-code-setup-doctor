using System.Windows;
using SetupDoctor.App.ViewModels;
using SetupDoctor.Core.Abstractions;
using SetupDoctor.Core.Diagnostics;
using SetupDoctor.Core.Diagnostics.Checks;
using SetupDoctor.Core.Policies;
using SetupDoctor.Core.Remediation;
using SetupDoctor.Infrastructure.Windows.Environment;
using SetupDoctor.Infrastructure.Windows.Files;
using SetupDoctor.Infrastructure.Windows.Network;
using SetupDoctor.Infrastructure.Windows.Processes;
using SetupDoctor.Infrastructure.Windows.Remediation;
using SetupDoctor.Infrastructure.Windows.WindowsApi;
using SetupDoctor.Reporting;

namespace SetupDoctor.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // コンポジションルート（手動DIワイヤリング）
        IClock clock = new SystemClock();
        ICommandRunner runner = new WindowsCommandRunner();
        ISystemInfoProvider sysInfo = new WindowsSystemInfoProvider();
        IPathEnvironmentService pathSvc = new WindowsPathEnvironmentService();
        IClaudeSettingsService claudeSettings = new WindowsClaudeSettingsService();
        IFileBackupService backupSvc = new WindowsFileBackupService();
        INetworkProbe networkProbe = new WindowsNetworkProbe();
        IReportWriter reportWriter = new JsonReportWriter();

        var checks = BuildChecks(runner, sysInfo, pathSvc, claudeSettings, networkProbe, clock);
        var aggregator = new ReadinessAggregator();
        IDiagnosticOrchestrator orchestrator = new DiagnosticOrchestrator(checks, aggregator, clock);

        var remediationActions = BuildRemediationActions(pathSvc, sysInfo, claudeSettings, backupSvc);
        IRemediationOrchestrator remediationOrchestrator = new RemediationOrchestrator(remediationActions.Values);

        var mainVm = new MainViewModel(orchestrator, remediationOrchestrator, remediationActions, reportWriter);
        var mainWindow = new MainWindow { DataContext = mainVm };
        mainWindow.Show();
    }

    private static IReadOnlyDictionary<string, IRemediationAction> BuildRemediationActions(
        IPathEnvironmentService pathSvc,
        ISystemInfoProvider sysInfo,
        IClaudeSettingsService claudeSettings,
        IFileBackupService backupSvc)
    {
        IRemediationAction[] actions =
        [
            new AddClaudeToPathAction(pathSvc, sysInfo),
            new SetGitBashPathAction(claudeSettings, backupSvc),
        ];
        return actions.ToDictionary(a => a.Id);
    }

    private static IReadOnlyList<IDiagnosticCheck> BuildChecks(
        ICommandRunner runner,
        ISystemInfoProvider sysInfo,
        IPathEnvironmentService pathSvc,
        IClaudeSettingsService claudeSettings,
        INetworkProbe networkProbe,
        IClock clock)
    {
        return new IDiagnosticCheck[]
        {
            // システム
            new OsVersionCheck(sysInfo, clock),
            new ArchitectureCheck(sysInfo, clock),
            new MemoryCheck(sysInfo, clock),
            // シェル
            new WindowsPowerShellCheck(runner, clock),
            new PowerShell7Check(runner, clock),
            // Claude
            new ClaudeCandidateDiscovery(runner, sysInfo, clock),
            new ClaudeVersionCheck(runner, sysInfo, clock),
            new ClaudeDesktopConflictCheck(runner, sysInfo, clock),
            new ClaudeMultipleInstallationsCheck(runner, sysInfo, clock),
            // PATH
            new ClaudeUserPathCheck(pathSvc, sysInfo, clock),
            // Git
            new GitCheck(runner, clock),
            new GitBashCheck(runner, claudeSettings, clock),
            // 認証
            new AuthStatusCheck(runner, sysInfo, clock),
            // ネットワーク
            new NetworkProbeCheck("CHK-NET-001", "api.anthropic.com", networkProbe, clock),
            new NetworkProbeCheck("CHK-NET-002", "statsig.anthropic.com", networkProbe, clock),
            new NetworkProbeCheck("CHK-NET-003", "sentry.io", networkProbe, clock),
            // 補助
            new WinGetCheck(runner, clock),
            new ClaudeDoctorCheck(runner, sysInfo, clock),
        };
    }
}

internal sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
