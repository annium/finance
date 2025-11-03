using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Annium.Finance.Providers.Tests.Shared;

/// <summary>
/// Contains unit tests for <see cref="AsyncLazy{T}"/> to verify lazy initialization behavior.
/// </summary>
public class DebugTest(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task Should_Connect_To_Binance_Via_HttpProxy()
    {
        const string proxyHost = "127.0.0.1";
        const int proxyPort = 9090;
        const string targetHost = "stream.binance.com";
        const int targetPort = 443;
        var ct = TestContext.Current.CancellationToken;

        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(proxyHost, proxyPort, ct);
        await using var proxyStream = tcpClient.GetStream();

        var connectRequest = Encoding.ASCII.GetBytes(
            $"CONNECT {targetHost}:{targetPort} HTTP/1.1\r\n" + $"Host: {targetHost}:{targetPort}\r\n\r\n"
        );

        await proxyStream.WriteAsync(connectRequest, 0, connectRequest.Length, ct);
        await proxyStream.FlushAsync(ct);

        var buffer = new byte[128];
        var bytesRead = await proxyStream.ReadAsync(buffer, 0, buffer.Length, ct);

        Assert.True(bytesRead > 0, "Proxy response is empty.");

        var response = Encoding.ASCII.GetString(buffer, 0, bytesRead);
        outputHelper.WriteLine(response);

        Assert.StartsWith("HTTP/", response);
        Assert.Contains(" 200", response);
    }

    [Fact]
    public async Task Should_Connect_To_Binance_Without_HttpProxy()
    {
        const string targetHost = "stream.binance.com";
        const int targetPort = 443;
        var ct = TestContext.Current.CancellationToken;

        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(targetHost, targetPort, ct);
        await using var networkStream = tcpClient.GetStream();
        await using var sslStream = new SslStream(networkStream, leaveInnerStreamOpen: false);
        var authOptions = new SslClientAuthenticationOptions { TargetHost = targetHost };
        await sslStream.AuthenticateAsClientAsync(authOptions, ct);
        Assert.True(sslStream.IsAuthenticated, "TLS handshake with Binance failed.");
        Assert.True(sslStream.CanRead && sslStream.CanWrite, "TLS stream is not usable after authentication.");
    }
}
