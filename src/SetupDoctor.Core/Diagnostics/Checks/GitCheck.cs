using SetupDoctor.Core.Abstractions;
using SetupDoctor.Core.Models;

namespace SetupDoctor.Core.Diagnostics.Checks;

// CHK-GIT-001: Git コマンドの存在確認。PowerShellが使えるなら必須エラーにしない。
public sealed class GitCheck : IDiagnosticCheck
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly ICommandRunner _runner;
    private readonly IClock _clock;

    public string Id => "CHK-GIT-001";

    public GitCheck(ICommandRunner runner, IClock clock)
    {
        _runner = runner;
        _clock = clock;
    }

    public async Task<DiagnosticResult> RunAsync(DiagnosticContext context, CancellationToken cancellationToken)
    {
        var start = _clock.UtcNow;

        try
        {
            var result = await _runner.RunAsync(
                new CommandRequest("git.exe", ["--version"], @"C:\", Timeout),
                cancellationToken);

            if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.StandardOutput))
            {
                return Mk(DiagnosticStatus.Pass, "GIT_AVAILABLE", "GIT_VERSION_OK", start,
                    new Dictionary<string, string> { ["gitVersion"] = result.StandardOutput.Trim() });
            }
        }
        catch { /* git不在として継続 */ }

        // Git不在のとき: PowerShellが使えるならWarning、両方ないならFail
        var psResult = context.GetPrior("CHK-SHELL-001");
        var psAvailable = psResult?.Status == DiagnosticStatus.Pass;

        var status = psAvailable ? DiagnosticStatus.Warning : DiagnosticStatus.Fail;
        return Mk(status, "GIT_NOT_FOUND", psAvailable ? "GIT_PS_AVAILABLE" : "GIT_NO_SHELL", start);
    }

    private DiagnosticResult Mk(DiagnosticStatus status, string summary, string detail,
        DateTimeOffset start, Dictionary<string, string>? meta = null)
        => new(Id, status, summary, detail,
            meta ?? new Dictionary<string, string>(),
            _clock.UtcNow - start, _clock.UtcNow);
}
