using SetupDoctor.Core.Abstractions;
using SetupDoctor.Core.Models;

namespace SetupDoctor.Core.Diagnostics.Checks;

// CHK-CLAUDE-003: WindowsApps Desktop alias競合を検出する
public sealed class ClaudeDesktopConflictCheck : IDiagnosticCheck
{
    private const string WindowsAppsFragment = @"\Microsoft\WindowsApps\";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly ICommandRunner _runner;
    private readonly ISystemInfoProvider _sys;
    private readonly IClock _clock;

    public string Id => "CHK-CLAUDE-003";

    public ClaudeDesktopConflictCheck(ICommandRunner runner, ISystemInfoProvider sys, IClock clock)
    {
        _runner = runner;
        _sys = sys;
        _clock = clock;
    }

    public async Task<DiagnosticResult> RunAsync(DiagnosticContext context, CancellationToken cancellationToken)
    {
        var start = _clock.UtcNow;

        // where.exeで先頭候補を取得
        CommandResult whereResult;
        try
        {
            whereResult = await _runner.RunAsync(
                new CommandRequest("where.exe", ["claude"], @"C:\", Timeout),
                cancellationToken);
        }
        catch
        {
            return Mk(DiagnosticStatus.NotApplicable, "DESKTOP_CONFLICT_SKIPPED", "WHERE_UNAVAILABLE", start);
        }

        if (whereResult.ExitCode != 0 || string.IsNullOrWhiteSpace(whereResult.StandardOutput))
            return Mk(DiagnosticStatus.NotApplicable, "DESKTOP_CONFLICT_NO_CANDIDATES", "CLAUDE_NOT_IN_PATH", start);

        var firstCandidate = whereResult.StandardOutput
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()?.Trim();

        if (firstCandidate is null)
            return Mk(DiagnosticStatus.NotApplicable, "DESKTOP_CONFLICT_NO_CANDIDATES", "CLAUDE_NOT_IN_PATH", start);

        // 先頭候補がWindowsApps配下かどうか確認
        if (firstCandidate.Contains(WindowsAppsFragment, StringComparison.OrdinalIgnoreCase))
        {
            // バージョン実行を試みる（タイムアウトかGUI起動になるかを確認）
            var versionResult = await _runner.RunAsync(
                new CommandRequest(firstCandidate, ["--version"], @"C:\",
                    TimeSpan.FromSeconds(3)),
                cancellationToken);

            if (versionResult.TimedOut || string.IsNullOrWhiteSpace(versionResult.StandardOutput))
            {
                return Mk(DiagnosticStatus.UserAction, "DESKTOP_CONFLICT_DETECTED",
                    "DESKTOP_ALIAS_BLOCKS_CLI", start,
                    new Dictionary<string, string>
                    {
                        ["firstCandidateMasked"] = "[WindowsApps/claude]",
                        ["nativePathExists"] = File.Exists(
                            _sys.ExpandEnvironmentVariables(@"%USERPROFILE%\.local\bin\claude.exe"))
                            .ToString(),
                    });
            }
        }

        return Mk(DiagnosticStatus.NotApplicable, "DESKTOP_CONFLICT_NOT_DETECTED", "NO_CONFLICT", start);
    }

    private DiagnosticResult Mk(DiagnosticStatus status, string summary, string detail,
        DateTimeOffset start, Dictionary<string, string>? meta = null)
        => new(Id, status, summary, detail,
            meta ?? new Dictionary<string, string>(),
            _clock.UtcNow - start, _clock.UtcNow);
}
