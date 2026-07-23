using SetupDoctor.Core.Abstractions;
using SetupDoctor.Core.Policies;

namespace SetupDoctor.Core.Diagnostics.Checks;

// CHK-PATH-001: User PATHにClaudeネイティブパスが含まれているか確認する
public sealed class ClaudeUserPathCheck : IDiagnosticCheck
{
    private readonly IPathEnvironmentService _path;
    private readonly ISystemInfoProvider _sys;
    private readonly IClock _clock;

    public string Id => "CHK-PATH-001";

    public ClaudeUserPathCheck(IPathEnvironmentService path, ISystemInfoProvider sys, IClock clock)
    {
        _path = path;
        _sys = sys;
        _clock = clock;
    }

    public Task<DiagnosticResult> RunAsync(DiagnosticContext context, CancellationToken cancellationToken)
    {
        var start = _clock.UtcNow;

        var targetExpanded = _sys.ExpandEnvironmentVariables(@"%USERPROFILE%\.local\bin");
        var nativeExe = Path.Combine(targetExpanded, "claude.exe");

        // ネイティブ実行ファイルが存在しない場合はNotApplicable
        if (!File.Exists(nativeExe))
            return Task.FromResult(Mk(DiagnosticStatus.NotApplicable, "PATH_NATIVE_ABSENT",
                "CLAUDE_EXE_NOT_FOUND", start));

        var userPath = _path.GetUserPath();
        var processPath = _path.GetProcessPath();

        var inUserPath = PathNormalizer.Contains(userPath, targetExpanded);
        var inProcessPath = PathNormalizer.Contains(processPath, targetExpanded);

        if (inUserPath)
        {
            // User PATHにある。現プロセスにない場合はターミナル再起動が必要という警告のみ
            if (!inProcessPath)
                return Task.FromResult(Mk(DiagnosticStatus.Warning, "PATH_USER_SET_PROCESS_STALE",
                    "PATH_NEED_NEW_TERMINAL", start,
                    new Dictionary<string, string> { ["targetMasked"] = @"%USERPROFILE%\.local\bin" }));

            return Task.FromResult(Mk(DiagnosticStatus.Pass, "PATH_OK", "PATH_CLAUDE_IN_USER_PATH", start,
                new Dictionary<string, string> { ["targetMasked"] = @"%USERPROFILE%\.local\bin" }));
        }

        return Task.FromResult(Mk(DiagnosticStatus.Repairable, "PATH_CLAUDE_MISSING",
            "PATH_ADD_LOCALBIN", start,
            new Dictionary<string, string>
            {
                ["targetMasked"] = @"%USERPROFILE%\.local\bin",
                ["remediationId"] = "REM-PATH-CLAUDE-LOCALBIN",
            }));
    }

    private DiagnosticResult Mk(DiagnosticStatus status, string summary, string detail,
        DateTimeOffset start, Dictionary<string, string>? meta = null)
        => new(Id, status, summary, detail,
            meta ?? new Dictionary<string, string>(),
            _clock.UtcNow - start, _clock.UtcNow);
}
