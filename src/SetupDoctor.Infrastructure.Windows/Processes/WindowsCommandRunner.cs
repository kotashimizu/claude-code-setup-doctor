using System.Diagnostics;
using SetupDoctor.Core.Abstractions;
using SetupDoctor.Core.Models;

namespace SetupDoctor.Infrastructure.Windows.Processes;

public sealed class WindowsCommandRunner : ICommandRunner
{
    public async Task<CommandResult> RunAsync(CommandRequest request, CancellationToken cancellationToken)
    {
        // 相対パス禁止: フルパスまたはPATHに存在するコマンドのみ許可
        var psi = new ProcessStartInfo
        {
            FileName = request.ExecutablePath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = request.WorkingDirectory,
        };

        // ArgumentList を使いシェル文字列連結を避ける
        foreach (var arg in request.Arguments)
            psi.ArgumentList.Add(arg);

        var sw = Stopwatch.StartNew();
        using var process = new Process { StartInfo = psi };
        process.Start();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(request.Timeout);

        string stdout = string.Empty;
        string stderr = string.Empty;
        bool timedOut = false;

        try
        {
            var stdoutTask = process.StandardOutput.ReadToEndAsync(timeoutCts.Token);
            var stderrTask = process.StandardError.ReadToEndAsync(timeoutCts.Token);
            await process.WaitForExitAsync(timeoutCts.Token);
            stdout = await stdoutTask;
            stderr = await stderrTask;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            timedOut = true;
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch { /* プロセス終了済みの場合は無視 */ }
        }

        sw.Stop();

        return new CommandResult(
            timedOut ? -1 : process.ExitCode,
            stdout,
            stderr,
            sw.Elapsed,
            timedOut);
    }
}
