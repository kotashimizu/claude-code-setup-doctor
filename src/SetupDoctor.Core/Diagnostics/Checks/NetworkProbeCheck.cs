using SetupDoctor.Core.Abstractions;

namespace SetupDoctor.Core.Diagnostics.Checks;

// CHK-NET-001/002/003: ネットワーク到達性の確認（Optional・利用者が明示的に開始した場合のみ）
public sealed class NetworkProbeCheck : IDiagnosticCheck
{
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(5);

    private readonly string _host;
    private readonly INetworkProbe _probe;
    private readonly IClock _clock;

    public string Id { get; }

    public NetworkProbeCheck(string checkId, string host, INetworkProbe probe, IClock clock)
    {
        Id = checkId;
        _host = host;
        _probe = probe;
        _clock = clock;
    }

    public async Task<DiagnosticResult> RunAsync(DiagnosticContext context, CancellationToken cancellationToken)
    {
        var start = _clock.UtcNow;

        var result = await _probe.ProbeAsync(_host, ProbeTimeout, cancellationToken);

        var (status, summary, detail) = result.Status switch
        {
            NetworkProbeStatus.Reachable =>
                (DiagnosticStatus.Pass, "NET_REACHABLE", $"NET_{_host.ToUpperInvariant().Replace('.', '_')}_OK"),
            NetworkProbeStatus.TlsError =>
                (DiagnosticStatus.ITAction, "NET_TLS_ERROR", "NET_TLS_INSPECTION_POSSIBLE"),
            NetworkProbeStatus.DnsError =>
                (DiagnosticStatus.ITAction, "NET_DNS_ERROR", "NET_DNS_BLOCKED"),
            NetworkProbeStatus.Timeout =>
                (DiagnosticStatus.ITAction, "NET_TIMEOUT", "NET_PROXY_OR_FIREWALL"),
            NetworkProbeStatus.Blocked =>
                (DiagnosticStatus.ITAction, "NET_BLOCKED", "NET_FIREWALL_OR_POLICY"),
            _ => (DiagnosticStatus.Unknown, "NET_UNKNOWN", "NET_UNKNOWN_ERROR"),
        };

        return new(Id, status, summary, detail,
            new Dictionary<string, string> { ["host"] = _host },
            _clock.UtcNow - start, _clock.UtcNow);
    }
}
