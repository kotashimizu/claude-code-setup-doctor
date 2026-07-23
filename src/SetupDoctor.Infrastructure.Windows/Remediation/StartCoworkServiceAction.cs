using SetupDoctor.Core.Abstractions;
using SetupDoctor.Core.Models;
using SetupDoctor.Core.Remediation;

namespace SetupDoctor.Infrastructure.Windows.Remediation;

// REM-COWORK-START-SERVICE: CoworkVMServiceが停止している場合に起動する。
// サービス起動にはWindowsの既定権限モデル上、通常ユーザー権限では実行できないためUAC昇格が必要。
public sealed class StartCoworkServiceAction : IRemediationAction
{
    private const string ServiceName = "CoworkVMService";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    private readonly ICommandRunner _runner;
    private readonly IElevatedCommandRunner _elevatedRunner;

    public string Id => "REM-COWORK-START-SERVICE";

    public StartCoworkServiceAction(ICommandRunner runner, IElevatedCommandRunner elevatedRunner)
    {
        _runner = runner;
        _elevatedRunner = elevatedRunner;
    }

    public async Task<RemediationPreview> PreviewAsync(RemediationContext context,
        CancellationToken cancellationToken)
    {
        var status = await GetServiceStatusAsync(cancellationToken);

        var plan = new RemediationPlanItem(
            Id,
            "Coworkの仮想マシンサービスを起動",
            $"{ServiceName} (Windowsサービス)",
            $"現在の状態: {status}",
            "起動 (Running)",
            RequiresElevation: true,
            RequiresNewProcess: false,
            SupportsRollback: false);

        var isFeasible = status.Equals("Stopped", StringComparison.OrdinalIgnoreCase);
        var note = isFeasible
            ? null
            : status.Equals("NOTFOUND", StringComparison.OrdinalIgnoreCase)
                ? $"{ServiceName} が見つかりません。Claude DesktopがCoworkに対応したバージョンか確認してください。"
                : $"サービスは既に '{status}' 状態のため、この修復は不要です。";

        return new RemediationPreview(plan, isFeasible, note);
    }

    public async Task<RemediationExecutionResult> ExecuteAsync(RemediationContext context,
        CancellationToken cancellationToken)
    {
        var status = await GetServiceStatusAsync(cancellationToken);
        if (!status.Equals("Stopped", StringComparison.OrdinalIgnoreCase))
        {
            return new RemediationExecutionResult(Id, RemediationStatus.Failed,
                $"サービスが 'Stopped' 状態ではありません（現在: {status}）。", false);
        }

        int exitCode;
        try
        {
            exitCode = await _elevatedRunner.RunElevatedAsync(
                "powershell.exe",
                ["-NoProfile", "-NonInteractive", "-Command", $"Start-Service -Name '{ServiceName}'"],
                Timeout,
                cancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            return new RemediationExecutionResult(Id, RemediationStatus.Cancelled, ex.Message, false);
        }

        if (exitCode != 0)
        {
            return new RemediationExecutionResult(Id, RemediationStatus.Failed,
                $"サービス起動に失敗しました（終了コード {exitCode}）。", false);
        }

        var afterStatus = await GetServiceStatusAsync(cancellationToken);
        if (!afterStatus.Equals("Running", StringComparison.OrdinalIgnoreCase))
        {
            return new RemediationExecutionResult(Id, RemediationStatus.Failed,
                $"起動コマンドは成功しましたが、状態が 'Running' になりませんでした（現在: {afterStatus}）。", false);
        }

        return new RemediationExecutionResult(Id, RemediationStatus.Verified, null, false);
    }

    public Task<RollbackResult> RollbackAsync(RemediationContext context, CancellationToken cancellationToken)
        => Task.FromResult(new RollbackResult(Id, true,
            "この修復にロールバックはありません（サービスは起動したままで問題ありません）。"));

    private async Task<string> GetServiceStatusAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _runner.RunAsync(
                new CommandRequest("powershell.exe",
                    ["-NoProfile", "-NonInteractive", "-Command",
                        $"$s = Get-Service -Name '{ServiceName}' -ErrorAction SilentlyContinue; " +
                        "if ($null -eq $s) { 'NOTFOUND' } else { $s.Status }"],
                    @"C:\", TimeSpan.FromSeconds(5)),
                cancellationToken);

            var status = result.StandardOutput.Trim();
            return string.IsNullOrEmpty(status) ? "NOTFOUND" : status;
        }
        catch
        {
            return "NOTFOUND";
        }
    }
}
