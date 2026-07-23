using SetupDoctor.Core.Abstractions;
using SetupDoctor.Core.Models;

namespace SetupDoctor.Core.Diagnostics.Checks;

// CHK-CLAUDE-004: 複数インストールの検出（自動修復しない）
public sealed class ClaudeMultipleInstallationsCheck : IDiagnosticCheck
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly ICommandRunner _runner;
    private readonly ISystemInfoProvider _sys;
    private readonly IClock _clock;

    public string Id => "CHK-CLAUDE-004";

    public ClaudeMultipleInstallationsCheck(ICommandRunner runner, ISystemInfoProvider sys, IClock clock)
    {
        _runner = runner;
        _sys = sys;
        _clock = clock;
    }

    public async Task<DiagnosticResult> RunAsync(DiagnosticContext context, CancellationToken cancellationToken)
    {
        var start = _clock.UtcNow;

        var candidates = new List<string>();

        try
        {
            var whereResult = await _runner.RunAsync(
                new CommandRequest("where.exe", ["claude"], @"C:\", Timeout),
                cancellationToken);

            if (whereResult.ExitCode == 0 && !string.IsNullOrWhiteSpace(whereResult.StandardOutput))
            {
                candidates.AddRange(whereResult.StandardOutput
                    .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.Trim())
                    .Where(l => l.Length > 0));
            }
        }
        catch { /* 候補なしとして継続 */ }

        // ネイティブパスを追加
        var nativePath = _sys.ExpandEnvironmentVariables(@"%USERPROFILE%\.local\bin\claude.exe");
        if (File.Exists(nativePath) &&
            !candidates.Any(c => c.Equals(nativePath, StringComparison.OrdinalIgnoreCase)))
            candidates.Add(nativePath);

        if (candidates.Count >= 2)
        {
            return Mk(DiagnosticStatus.Warning, "CLAUDE_MULTIPLE_FOUND", "CLAUDE_DUPLICATE_CANDIDATES", start,
                new Dictionary<string, string>
                {
                    ["candidateCount"] = candidates.Count.ToString(),
                    // パスはマスク
                });
        }

        return Mk(DiagnosticStatus.Pass, "CLAUDE_SINGLE_INSTALLATION", "CLAUDE_NO_DUPLICATE", start,
            new Dictionary<string, string> { ["candidateCount"] = candidates.Count.ToString() });
    }

    private DiagnosticResult Mk(DiagnosticStatus status, string summary, string detail,
        DateTimeOffset start, Dictionary<string, string>? meta = null)
        => new(Id, status, summary, detail,
            meta ?? new Dictionary<string, string>(),
            _clock.UtcNow - start, _clock.UtcNow);
}
