using System.Net.NetworkInformation;
using System.Net.Sockets;
using SetupDoctor.Core.Abstractions;

namespace SetupDoctor.Core.Diagnostics.Checks;

// CHK-COWORK-005: CoworkのVMネットワーク(172.16.0.0/24)とVPN/Dockerのサブネット競合を検出する。
// 読み取り専用。NAT設定・仮想スイッチの直接操作はしない(自動修復なし)。
public sealed class CoworkNetworkConflictCheck : IDiagnosticCheck
{
    private readonly IClock _clock;

    public string Id => "CHK-COWORK-005";

    public CoworkNetworkConflictCheck(IClock clock)
    {
        _clock = clock;
    }

    public Task<DiagnosticResult> RunAsync(DiagnosticContext context, CancellationToken cancellationToken)
    {
        var start = _clock.UtcNow;

        try
        {
            var conflictingAdapterNames = new List<string>();

            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up) continue;

                foreach (var addr in nic.GetIPProperties().UnicastAddresses)
                {
                    if (addr.Address.AddressFamily != AddressFamily.InterNetwork) continue;

                    var octets = addr.Address.GetAddressBytes();
                    // 172.16.0.0/24 (Coworkの内部NAT) または 172.17.0.0/16 (Docker既定ブリッジ) と重複するか
                    var isCoworkRange = octets[0] == 172 && octets[1] == 16;
                    var isDockerRange = octets[0] == 172 && octets[1] == 17;

                    if (isCoworkRange || isDockerRange)
                        conflictingAdapterNames.Add(nic.Name);
                }
            }

            if (conflictingAdapterNames.Count == 0)
                return Task.FromResult(Mk(DiagnosticStatus.Pass, "COWORK_NET_NO_CONFLICT", "COWORK_NET_OK", start));

            return Task.FromResult(Mk(DiagnosticStatus.ITAction, "COWORK_NET_SUBNET_CONFLICT",
                "COWORK_NET_172_RANGE_OVERLAP", start,
                new Dictionary<string, string>
                {
                    ["conflictingAdapterCount"] = conflictingAdapterNames.Count.ToString(),
                }));
        }
        catch
        {
            return Task.FromResult(Mk(DiagnosticStatus.Unknown, "COWORK_NET_CHECK_FAILED",
                "COWORK_NET_INTERFACE_UNREADABLE", start));
        }
    }

    private DiagnosticResult Mk(DiagnosticStatus status, string summary, string detail,
        DateTimeOffset start, Dictionary<string, string>? meta = null)
        => new(Id, status, summary, detail,
            meta ?? new Dictionary<string, string>(),
            _clock.UtcNow - start, _clock.UtcNow);
}
