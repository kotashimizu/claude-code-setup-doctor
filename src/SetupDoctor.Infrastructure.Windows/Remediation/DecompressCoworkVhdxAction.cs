using SetupDoctor.Core.Abstractions;
using SetupDoctor.Core.Policies;
using SetupDoctor.Core.Remediation;

namespace SetupDoctor.Infrastructure.Windows.Remediation;

// REM-COWORK-DECOMPRESS-VHDX: Cowork VMバンドルフォルダのNTFS圧縮属性を解除する。
// サービス停止→圧縮解除→サービス再起動を1回のUAC昇格でまとめて実行する。
public sealed class DecompressCoworkVhdxAction : IRemediationAction
{
    private const string ServiceName = "CoworkVMService";
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(3);

    private readonly ISystemInfoProvider _sys;
    private readonly IElevatedCommandRunner _elevatedRunner;

    public string Id => "REM-COWORK-DECOMPRESS-VHDX";

    public DecompressCoworkVhdxAction(ISystemInfoProvider sys, IElevatedCommandRunner elevatedRunner)
    {
        _sys = sys;
        _elevatedRunner = elevatedRunner;
    }

    public Task<RemediationPreview> PreviewAsync(RemediationContext context, CancellationToken cancellationToken)
    {
        var vmBundlesDir = CoworkPaths.ResolveVmBundlesDirectory(_sys);

        var plan = new RemediationPlanItem(
            Id,
            "Cowork仮想ディスクのNTFS圧縮を解除",
            vmBundlesDir ?? "Cowork VMバンドルフォルダ",
            "NTFS圧縮属性: あり",
            "NTFS圧縮属性: なし",
            RequiresElevation: true,
            RequiresNewProcess: false,
            SupportsRollback: false);

        if (vmBundlesDir is null)
            return Task.FromResult(new RemediationPreview(plan, false,
                "Cowork VMバンドルフォルダが見つかりません。Coworkが未初期化の可能性があります。"));

        bool isCompressed;
        try
        {
            var attributes = File.GetAttributes(vmBundlesDir);
            isCompressed = (attributes & FileAttributes.Compressed) == FileAttributes.Compressed;
        }
        catch
        {
            return Task.FromResult(new RemediationPreview(plan, false, "フォルダの属性を読み取れませんでした。"));
        }

        return Task.FromResult(new RemediationPreview(plan, isCompressed,
            isCompressed ? null : "既にNTFS圧縮は解除されているため、この修復は不要です。"));
    }

    public async Task<RemediationExecutionResult> ExecuteAsync(RemediationContext context,
        CancellationToken cancellationToken)
    {
        var vmBundlesDir = CoworkPaths.ResolveVmBundlesDirectory(_sys);
        if (vmBundlesDir is null)
            return new RemediationExecutionResult(Id, RemediationStatus.Failed,
                "Cowork VMバンドルフォルダが見つかりませんでした。", false);

        var escapedPath = vmBundlesDir.Replace("'", "''");
        var script = $"Stop-Service -Name '{ServiceName}' -Force -ErrorAction Stop; " +
                     $"compact /u /s:'{escapedPath}'; " +
                     $"Start-Service -Name '{ServiceName}'";

        int exitCode;
        try
        {
            exitCode = await _elevatedRunner.RunElevatedAsync(
                "powershell.exe",
                ["-NoProfile", "-NonInteractive", "-Command", script],
                Timeout,
                cancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            return new RemediationExecutionResult(Id, RemediationStatus.Cancelled, ex.Message, false);
        }

        if (exitCode != 0)
            return new RemediationExecutionResult(Id, RemediationStatus.Failed,
                $"圧縮解除処理が失敗しました（終了コード {exitCode}）。", false);

        try
        {
            var attributes = File.GetAttributes(vmBundlesDir);
            if ((attributes & FileAttributes.Compressed) == FileAttributes.Compressed)
                return new RemediationExecutionResult(Id, RemediationStatus.Failed,
                    "処理は完了しましたが、圧縮属性がまだ残っています。", false);
        }
        catch
        {
            return new RemediationExecutionResult(Id, RemediationStatus.Failed,
                "処理後の属性確認に失敗しました。", false);
        }

        return new RemediationExecutionResult(Id, RemediationStatus.Verified, null, false);
    }

    public Task<RollbackResult> RollbackAsync(RemediationContext context, CancellationToken cancellationToken)
        => Task.FromResult(new RollbackResult(Id, true,
            "この修復にロールバックはありません（圧縮を元に戻す必要はありません）。"));
}
