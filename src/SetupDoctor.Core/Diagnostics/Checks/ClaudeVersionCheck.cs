using SetupDoctor.Core.Abstractions;
using SetupDoctor.Core.Models;

namespace SetupDoctor.Core.Diagnostics.Checks;

// CHK-CLAUDE-002: --version を実行してバージョン取得を確認する
public sealed class ClaudeVersionCheck : IDiagnosticCheck
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
    // WindowsAppsパス: Desktop alias競合の検出に使う
    private const string WindowsAppsPathFragment = @"\WindowsApps\";

    private readonly ICommandRunner _runner;
    private readonly ISystemInfoProvider _sys;
    private readonly IClock _clock;

    public string Id => "CHK-CLAUDE-002";

    public ClaudeVersionCheck(ICommandRunner runner, ISystemInfoProvider sys, IClock clock)
    {
        _runner = runner;
        _sys = sys;
        _clock = clock;
    }

    public async Task<DiagnosticResult> RunAsync(DiagnosticContext context, CancellationToken cancellationToken)
    {
        var start = _clock.UtcNow;

        // CHK-CLAUDE-001の結果からPassになったことを確認
        var discovery = context.GetPrior("CHK-CLAUDE-001");
        if (discovery is null || discovery.Status == DiagnosticStatus.Fail)
            return Mk(DiagnosticStatus.NotApplicable, "CLAUDE_VERSION_SKIPPED", "CLAUDE_NOT_FOUND", start);

        var nativePath = _sys.ExpandEnvironmentVariables(@"%USERPROFILE%\.local\bin\claude.exe");
        var executablePath = File.Exists(nativePath) ? nativePath : "claude.exe";

        CommandResult result;
        try
        {
            result = await _runner.RunAsync(
                new CommandRequest(executablePath, ["--version"], @"C:\", Timeout),
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return Mk(DiagnosticStatus.Unknown, "CLAUDE_VERSION_TIMEOUT", "CLAUDE_TIMEOUT", start);
        }

        if (result.TimedOut)
        {
            // タイムアウトはDesktop alias競合の可能性
            return Mk(DiagnosticStatus.Unknown, "CLAUDE_VERSION_TIMEOUT",
                "CLAUDE_TIMEOUT_MAYBE_DESKTOP_ALIAS", start);
        }

        if (result.ExitCode != 0 || string.IsNullOrWhiteSpace(result.StandardOutput))
            return Mk(DiagnosticStatus.Fail, "CLAUDE_VERSION_FAILED", "CLAUDE_EXIT_NONZERO", start,
                new Dictionary<string, string> { ["exitCode"] = result.ExitCode.ToString() });

        var version = result.StandardOutput.Trim();
        return Mk(DiagnosticStatus.Pass, "CLAUDE_VERSION_OK", "CLAUDE_VERSION_RETRIEVED", start,
            new Dictionary<string, string> { ["version"] = version });
    }

    private DiagnosticResult Mk(DiagnosticStatus status, string summary, string detail,
        DateTimeOffset start, Dictionary<string, string>? meta = null)
        => new(Id, status, summary, detail,
            meta ?? new Dictionary<string, string>(),
            _clock.UtcNow - start, _clock.UtcNow);
}
