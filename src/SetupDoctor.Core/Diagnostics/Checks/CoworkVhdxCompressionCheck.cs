using SetupDoctor.Core.Abstractions;
using SetupDoctor.Core.Policies;

namespace SetupDoctor.Core.Diagnostics.Checks;

// CHK-COWORK-004: Cowork VM仮想ディスク(vhdx)を格納するフォルダのNTFS圧縮属性。
// Claude Desktop v1.24012.0(2026-07-21)で修正済みの既知バグへの対処。読み取り専用。
public sealed class CoworkVhdxCompressionCheck : IDiagnosticCheck
{
    private readonly ISystemInfoProvider _sys;
    private readonly IClock _clock;

    public string Id => "CHK-COWORK-004";

    public CoworkVhdxCompressionCheck(ISystemInfoProvider sys, IClock clock)
    {
        _sys = sys;
        _clock = clock;
    }

    public Task<DiagnosticResult> RunAsync(DiagnosticContext context, CancellationToken cancellationToken)
    {
        var start = _clock.UtcNow;

        var vmBundlesDir = CoworkPaths.ResolveVmBundlesDirectory(_sys);
        if (vmBundlesDir is null)
            return Task.FromResult(Mk(DiagnosticStatus.Unknown, "COWORK_VHDX_DIR_NOT_FOUND",
                "COWORK_VM_BUNDLES_ABSENT", start));

        try
        {
            var attributes = File.GetAttributes(vmBundlesDir);
            var isCompressed = (attributes & FileAttributes.Compressed) == FileAttributes.Compressed;

            if (!isCompressed)
                return Task.FromResult(Mk(DiagnosticStatus.Pass, "COWORK_VHDX_NOT_COMPRESSED",
                    "COWORK_VHDX_OK", start));

            return Task.FromResult(Mk(DiagnosticStatus.Repairable, "COWORK_VHDX_COMPRESSED",
                "COWORK_VHDX_COMPRESSION_DETECTED", start,
                new Dictionary<string, string> { ["remediationId"] = "REM-COWORK-DECOMPRESS-VHDX" }));
        }
        catch
        {
            return Task.FromResult(Mk(DiagnosticStatus.Unknown, "COWORK_VHDX_CHECK_FAILED",
                "COWORK_ATTRIBUTES_UNREADABLE", start));
        }
    }

    private DiagnosticResult Mk(DiagnosticStatus status, string summary, string detail,
        DateTimeOffset start, Dictionary<string, string>? meta = null)
        => new(Id, status, summary, detail,
            meta ?? new Dictionary<string, string>(),
            _clock.UtcNow - start, _clock.UtcNow);
}
