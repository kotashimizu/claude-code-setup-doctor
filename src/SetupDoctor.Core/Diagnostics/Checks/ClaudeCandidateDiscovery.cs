using SetupDoctor.Core.Abstractions;
using SetupDoctor.Core.Models;

namespace SetupDoctor.Core.Diagnostics.Checks;

// CHK-CLAUDE-001: PATH上のclaudeを列挙し、ネイティブパスの存在も確認する
public sealed class ClaudeCandidateDiscovery : IDiagnosticCheck
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly ICommandRunner _runner;
    private readonly ISystemInfoProvider _sys;
    private readonly IClock _clock;

    public string Id => "CHK-CLAUDE-001";

    public ClaudeCandidateDiscovery(ICommandRunner runner, ISystemInfoProvider sys, IClock clock)
    {
        _runner = runner;
        _sys = sys;
        _clock = clock;
    }

    public async Task<DiagnosticResult> RunAsync(DiagnosticContext context, CancellationToken cancellationToken)
    {
        var start = _clock.UtcNow;

        var nativePath = _sys.ExpandEnvironmentVariables(@"%USERPROFILE%\.local\bin\claude.exe");
        var nativeExists = File.Exists(nativePath);

        // where.exe で PATH上の候補を探す
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
        catch { /* where.exe失敗は候補なしとして継続 */ }

        // ネイティブパスを候補に追加（重複しない場合）
        if (nativeExists && !candidates.Any(c =>
            c.Equals(nativePath, StringComparison.OrdinalIgnoreCase)))
        {
            candidates.Add(nativePath);
        }

        if (candidates.Count == 0)
        {
            return Mk(DiagnosticStatus.Fail, "CLAUDE_NOT_FOUND", "CLAUDE_NO_CANDIDATES", start,
                new Dictionary<string, string> { ["nativePathExists"] = nativeExists.ToString() });
        }

        // 候補がネイティブパスにのみ存在するがPATHにない場合はRepairable
        var inPath = candidates.Any(c =>
            !c.Equals(nativePath, StringComparison.OrdinalIgnoreCase));

        if (!inPath && nativeExists)
        {
            return Mk(DiagnosticStatus.Repairable, "CLAUDE_NATIVE_NOT_IN_PATH", "CLAUDE_ADD_TO_PATH", start,
                new Dictionary<string, string>
                {
                    ["candidateCount"] = candidates.Count.ToString(),
                    ["nativeExists"] = "true",
                });
        }

        return Mk(DiagnosticStatus.Pass, "CLAUDE_FOUND", "CLAUDE_CANDIDATES_OK", start,
            new Dictionary<string, string>
            {
                ["candidateCount"] = candidates.Count.ToString(),
                ["nativeExists"] = nativeExists.ToString(),
            });
    }

    private DiagnosticResult Mk(DiagnosticStatus status, string summary, string detail,
        DateTimeOffset start, Dictionary<string, string>? meta = null)
        => new(Id, status, summary, detail,
            meta ?? new Dictionary<string, string>(),
            _clock.UtcNow - start, _clock.UtcNow);
}
