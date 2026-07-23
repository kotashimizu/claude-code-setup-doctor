using SetupDoctor.Core.Abstractions;
using SetupDoctor.Core.Models;

namespace SetupDoctor.Core.Diagnostics.Checks;

// CHK-AUTH-001: claude auth status で認証状態を確認する
public sealed class AuthStatusCheck : IDiagnosticCheck
{
    // 仕様: auth statusのみ10秒タイムアウト（Q-003 暫定判断）
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private readonly ICommandRunner _runner;
    private readonly ISystemInfoProvider _sys;
    private readonly IClock _clock;

    public string Id => "CHK-AUTH-001";

    public AuthStatusCheck(ICommandRunner runner, ISystemInfoProvider sys, IClock clock)
    {
        _runner = runner;
        _sys = sys;
        _clock = clock;
    }

    public async Task<DiagnosticResult> RunAsync(DiagnosticContext context, CancellationToken cancellationToken)
    {
        var start = _clock.UtcNow;

        var nativePath = _sys.ExpandEnvironmentVariables(@"%USERPROFILE%\.local\bin\claude.exe");
        var exe = File.Exists(nativePath) ? nativePath : "claude.exe";

        CommandResult result;
        try
        {
            result = await _runner.RunAsync(
                new CommandRequest(exe, ["auth", "status"], @"C:\", Timeout),
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return Mk(DiagnosticStatus.Unknown, "AUTH_CANCELLED", "AUTH_TIMEOUT", start);
        }

        if (result.TimedOut)
            return Mk(DiagnosticStatus.Unknown, "AUTH_TIMEOUT", "AUTH_NO_RESPONSE", start);

        return result.ExitCode switch
        {
            0 => Mk(DiagnosticStatus.Pass, "AUTH_OK", "AUTH_AUTHENTICATED", start),
            1 => Mk(DiagnosticStatus.UserAction, "AUTH_NOT_LOGGED_IN", "AUTH_LOGIN_REQUIRED", start,
                new Dictionary<string, string> { ["remediationId"] = "REM-AUTH-LOGIN" }),
            _ => Mk(DiagnosticStatus.Unknown, "AUTH_UNKNOWN", $"AUTH_EXIT_{result.ExitCode}", start),
        };
        // raw JSON/email/tokenは保存しない
    }

    private DiagnosticResult Mk(DiagnosticStatus status, string summary, string detail,
        DateTimeOffset start, Dictionary<string, string>? meta = null)
        => new(Id, status, summary, detail,
            meta ?? new Dictionary<string, string>(),
            _clock.UtcNow - start, _clock.UtcNow);
}
