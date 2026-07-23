using SetupDoctor.Core.Abstractions;

namespace SetupDoctor.Core.Diagnostics.Checks;

public sealed class ArchitectureCheck : IDiagnosticCheck
{
    private readonly ISystemInfoProvider _sys;
    private readonly IClock _clock;

    public string Id => "CHK-ARCH-001";

    public ArchitectureCheck(ISystemInfoProvider sys, IClock clock)
    {
        _sys = sys;
        _clock = clock;
    }

    public Task<DiagnosticResult> RunAsync(DiagnosticContext context, CancellationToken cancellationToken)
    {
        var start = _clock.UtcNow;
        var arch = _sys.Architecture?.ToUpperInvariant();

        if (!_sys.Is64BitOperatingSystem || (arch is not ("X64" or "AMD64" or "ARM64")))
        {
            return Task.FromResult(Result(DiagnosticStatus.Fail, "ARCH_NOT_64BIT", "ARCH_UNSUPPORTED", start,
                new Dictionary<string, string> { ["architecture"] = arch ?? "unknown" }));
        }

        return Task.FromResult(Result(DiagnosticStatus.Pass, "ARCH_SUPPORTED", $"ARCH_{arch}", start,
            new Dictionary<string, string> { ["architecture"] = arch ?? "unknown" }));
    }

    private DiagnosticResult Result(DiagnosticStatus status, string summary, string detail,
        DateTimeOffset start, Dictionary<string, string>? meta = null)
        => new(Id, status, summary, detail,
            meta ?? new Dictionary<string, string>(),
            _clock.UtcNow - start, _clock.UtcNow);
}
