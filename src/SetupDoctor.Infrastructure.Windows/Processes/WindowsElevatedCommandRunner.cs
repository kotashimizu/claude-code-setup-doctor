using System.ComponentModel;
using System.Diagnostics;
using SetupDoctor.Core.Abstractions;

namespace SetupDoctor.Infrastructure.Windows.Processes;

public sealed class WindowsElevatedCommandRunner : IElevatedCommandRunner
{
    private const int ErrorCancelled = 1223; // ユーザーがUACプロンプトで「いいえ」を選択

    public async Task<int> RunElevatedAsync(
        string executablePath,
        IReadOnlyList<string> arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = executablePath,
            UseShellExecute = true,
            Verb = "runas",
            WindowStyle = ProcessWindowStyle.Hidden,
        };
        foreach (var arg in arguments)
            psi.ArgumentList.Add(arg);

        using var process = new Process { StartInfo = psi };

        try
        {
            process.Start();
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == ErrorCancelled)
        {
            throw new OperationCanceledException("ユーザーが管理者権限の昇格を許可しませんでした。", ex);
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* 既に終了済みの場合は無視 */ }
            throw new TimeoutException($"昇格プロセスが{timeout.TotalSeconds}秒以内に終了しませんでした。");
        }

        return process.ExitCode;
    }
}
