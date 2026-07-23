using SetupDoctor.Core.Abstractions;
using SetupDoctor.Core.Models;

namespace SetupDoctor.Core.Diagnostics.Checks;

// CHK-GITBASH-001: Git Bash実行ファイルの存在と Claude設定の確認
public sealed class GitBashCheck : IDiagnosticCheck
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
    private const string DefaultBashPath = @"C:\Program Files\Git\bin\bash.exe";

    private readonly ICommandRunner _runner;
    private readonly IClaudeSettingsService _settings;
    private readonly IClock _clock;

    public string Id => "CHK-GITBASH-001";

    public GitBashCheck(ICommandRunner runner, IClaudeSettingsService settings, IClock clock)
    {
        _runner = runner;
        _settings = settings;
        _clock = clock;
    }

    public async Task<DiagnosticResult> RunAsync(DiagnosticContext context, CancellationToken cancellationToken)
    {
        var start = _clock.UtcNow;

        // 1. Claude設定からGit Bashパスを取得
        string? configuredPath = null;
        try { configuredPath = await _settings.GetGitBashPathAsync(cancellationToken); }
        catch { /* 設定取得失敗は無視 */ }

        // 2. 候補パスのリスト（優先順: 設定値 → 既定パス）
        var candidatePaths = new List<string>();
        if (!string.IsNullOrEmpty(configuredPath)) candidatePaths.Add(configuredPath!);
        if (File.Exists(DefaultBashPath)) candidatePaths.Add(DefaultBashPath);

        // 3. 動作確認
        foreach (var bashPath in candidatePaths)
        {
            if (!File.Exists(bashPath)) continue;

            try
            {
                var result = await _runner.RunAsync(
                    new CommandRequest(bashPath, ["--version"], @"C:\", Timeout),
                    cancellationToken);

                if (result.ExitCode == 0)
                {
                    // bash は動く。Claude設定に登録されているか確認
                    var isConfigured = bashPath.Equals(configuredPath, StringComparison.OrdinalIgnoreCase);
                    if (isConfigured)
                    {
                        return Mk(DiagnosticStatus.Pass, "GITBASH_CONFIGURED", "GITBASH_OK", start,
                            new Dictionary<string, string> { ["bashPathMasked"] = "[bash.exe]" });
                    }

                    return Mk(DiagnosticStatus.Repairable, "GITBASH_NOT_CONFIGURED",
                        "GITBASH_ADD_CLAUDE_SETTING", start,
                        new Dictionary<string, string>
                        {
                            ["bashPathMasked"] = "[bash.exe]",
                            ["remediationId"] = "REM-CLAUDE-GITBASH-SETTING",
                        });
                }
            }
            catch { /* この候補は動かない */ }
        }

        // Git Bashが見当たらない: PowerShellが使えるならWarning
        var psResult = context.GetPrior("CHK-SHELL-001");
        var psAvailable = psResult?.Status == DiagnosticStatus.Pass;

        return Mk(psAvailable ? DiagnosticStatus.Warning : DiagnosticStatus.Fail,
            "GITBASH_NOT_FOUND",
            psAvailable ? "GITBASH_PS_AVAILABLE" : "GITBASH_NO_SHELL",
            start);
    }

    private DiagnosticResult Mk(DiagnosticStatus status, string summary, string detail,
        DateTimeOffset start, Dictionary<string, string>? meta = null)
        => new(Id, status, summary, detail,
            meta ?? new Dictionary<string, string>(),
            _clock.UtcNow - start, _clock.UtcNow);
}
