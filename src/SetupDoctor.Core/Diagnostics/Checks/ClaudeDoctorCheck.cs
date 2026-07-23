using SetupDoctor.Core.Abstractions;
using SetupDoctor.Core.Models;

namespace SetupDoctor.Core.Diagnostics.Checks;

// CHK-DOCTOR-001: claude doctor を補助診断として実行する（Optional）
// 出力テキストは版依存なので全体判定に使わない
public sealed class ClaudeDoctorCheck : IDiagnosticCheck
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    private readonly ICommandRunner _runner;
    private readonly ISystemInfoProvider _sys;
    private readonly IClock _clock;

    public string Id => "CHK-DOCTOR-001";

    public ClaudeDoctorCheck(ICommandRunner runner, ISystemInfoProvider sys, IClock clock)
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
                new CommandRequest(exe, ["doctor"], @"C:\", Timeout),
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return Mk(DiagnosticStatus.Unknown, "DOCTOR_CANCELLED", "DOCTOR_TIMEOUT", start);
        }

        if (result.TimedOut)
            return Mk(DiagnosticStatus.Unknown, "DOCTOR_TIMEOUT", "DOCTOR_NO_RESPONSE", start);

        // 出力はPIIをRedactして記録するだけ。全体判定には使わない。
        return Mk(DiagnosticStatus.Pass, "DOCTOR_COMPLETED", $"DOCTOR_EXIT_{result.ExitCode}", start,
            new Dictionary<string, string>
            {
                ["exitCode"] = result.ExitCode.ToString(),
                // stdoutはPII除去済みの概要のみ
                ["outputLengthChars"] = result.StandardOutput.Length.ToString(),
            });
    }

    private DiagnosticResult Mk(DiagnosticStatus status, string summary, string detail,
        DateTimeOffset start, Dictionary<string, string>? meta = null)
        => new(Id, status, summary, detail,
            meta ?? new Dictionary<string, string>(),
            _clock.UtcNow - start, _clock.UtcNow);
}
