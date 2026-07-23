namespace SetupDoctor.Core.Abstractions;

public enum NetworkProbeStatus
{
    Reachable,
    Blocked,
    TlsError,
    DnsError,
    Timeout,
    Unknown,
}

public sealed record NetworkProbeResult(
    string Host,
    NetworkProbeStatus Status,
    string? ErrorDetail,
    TimeSpan Duration);

public interface INetworkProbe
{
    Task<NetworkProbeResult> ProbeAsync(string host, TimeSpan timeout, CancellationToken cancellationToken);
}
