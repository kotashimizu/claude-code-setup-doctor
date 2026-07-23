using SetupDoctor.Core.Abstractions;

namespace SetupDoctor.Core.Diagnostics.Checks;

public sealed class OsVersionCheck : IDiagnosticCheck
{
    // Windows 10 build 17763 = October 2018 Update (1809)
    private const int MinBuild = 17763;

    private readonly ISystemInfoProvider _sys;
    private readonly IClock _clock;

    public string Id => "CHK-OS-001";

    public OsVersionCheck(ISystemInfoProvider sys, IClock clock)
    {
        _sys = sys;
        _clock = clock;
    }

    public Task<DiagnosticResult> RunAsync(DiagnosticContext context, CancellationToken cancellationToken)
    {
        var start = _clock.UtcNow;

        var isWindows = _sys.OsFamily.Equals("Windows", StringComparison.OrdinalIgnoreCase);
        if (!isWindows)
        {
            return Task.FromResult(Result(DiagnosticStatus.Fail, "OS_NOT_WINDOWS", "OS_NOT_SUPPORTED", start));
        }

        var buildStr = _sys.OsBuild;
        if (int.TryParse(buildStr, out var build) && build >= MinBuild)
        {
            return Task.FromResult(Result(DiagnosticStatus.Pass, "OS_SUPPORTED",
                $"OS_WIN_BUILD_{build}", start,
                new Dictionary<string, string>
                {
                    ["osFamily"] = _sys.OsFamily,
                    ["osBuild"] = buildStr ?? "unknown",
                }));
        }

        return Task.FromResult(Result(DiagnosticStatus.Fail, "OS_VERSION_TOO_OLD", "OS_BUILD_BELOW_MIN", start,
            new Dictionary<string, string> { ["osBuild"] = buildStr ?? "unknown" }));
    }

    private DiagnosticResult Result(DiagnosticStatus status, string summary, string detail,
        DateTimeOffset start, Dictionary<string, string>? meta = null)
        => new(Id, status, summary, detail,
            meta ?? new Dictionary<string, string>(),
            _clock.UtcNow - start, _clock.UtcNow);
}
