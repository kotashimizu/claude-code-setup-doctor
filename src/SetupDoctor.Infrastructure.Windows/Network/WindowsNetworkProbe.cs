using System.Diagnostics;
using System.Net.Http;
using System.Net.Security;
using SetupDoctor.Core.Abstractions;

namespace SetupDoctor.Infrastructure.Windows.Network;

public sealed class WindowsNetworkProbe : INetworkProbe, IDisposable
{
    private readonly HttpClient _http;

    public WindowsNetworkProbe()
    {
        var handler = new HttpClientHandler
        {
            // TLS 証明書エラーを検出するために自動検証を維持する
            ServerCertificateCustomValidationCallback = (_, cert, chain, errors) =>
            {
                if (errors != SslPolicyErrors.None) return false;
                return true;
            },
        };
        _http = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
    }

    public async Task<NetworkProbeResult> ProbeAsync(string host, TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head,
                new Uri($"https://{host}/"));

            using var response = await _http.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead, cts.Token);

            sw.Stop();
            return new NetworkProbeResult(host, NetworkProbeStatus.Reachable, null, sw.Elapsed);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            sw.Stop();
            return new NetworkProbeResult(host, NetworkProbeStatus.Timeout, null, sw.Elapsed);
        }
        catch (HttpRequestException ex) when (
            ex.InnerException is System.Security.Authentication.AuthenticationException)
        {
            sw.Stop();
            return new NetworkProbeResult(host, NetworkProbeStatus.TlsError,
                ex.Message, sw.Elapsed);
        }
        catch (HttpRequestException ex) when (
            ex.InnerException is System.Net.Sockets.SocketException se
            && se.SocketErrorCode == System.Net.Sockets.SocketError.HostNotFound)
        {
            sw.Stop();
            return new NetworkProbeResult(host, NetworkProbeStatus.DnsError,
                ex.Message, sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new NetworkProbeResult(host, NetworkProbeStatus.Unknown,
                ex.GetType().Name, sw.Elapsed);
        }
    }

    public void Dispose() => _http.Dispose();
}
