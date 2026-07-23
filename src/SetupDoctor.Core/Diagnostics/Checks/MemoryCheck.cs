using SetupDoctor.Core.Abstractions;

namespace SetupDoctor.Core.Diagnostics.Checks;

public sealed class MemoryCheck : IDiagnosticCheck
{
    private const double MinMemoryGiB = 4.0;

    private readonly ISystemInfoProvider _sys;
    private readonly IClock _clock;

    public string Id => "CHK-MEM-001";

    public MemoryCheck(ISystemInfoProvider sys, IClock clock)
    {
        _sys = sys;
        _clock = clock;
    }

    public Task<DiagnosticResult> RunAsync(DiagnosticContext context, CancellationToken cancellationToken)
    {
        var start = _clock.UtcNow;

        var mem = _sys.MemoryGiB;
        if (mem is null)
            return Task.FromResult(Result(DiagnosticStatus.Unknown, "MEM_UNKNOWN", "MEM_API_UNAVAILABLE", start));

        var status = mem >= MinMemoryGiB ? DiagnosticStatus.Pass : DiagnosticStatus.Fail;
        var summary = status == DiagnosticStatus.Pass ? "MEM_SUFFICIENT" : "MEM_INSUFFICIENT";

        return Task.FromResult(Result(status, summary, $"MEM_{mem:F1}GIB", start,
            new Dictionary<string, string> { ["memoryGiB"] = $"{mem:F1}" }));
    }

    private DiagnosticResult Result(DiagnosticStatus status, string summary, string detail,
        DateTimeOffset start, Dictionary<string, string>? meta = null)
        => new(Id, status, summary, detail,
            meta ?? new Dictionary<string, string>(),
            _clock.UtcNow - start, _clock.UtcNow);
}
