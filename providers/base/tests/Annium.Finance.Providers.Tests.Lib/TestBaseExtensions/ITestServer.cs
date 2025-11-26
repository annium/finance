using System;
using System.Threading.Tasks;

namespace Annium.Finance.Providers.Tests.Lib.TestBaseExtensions;

public interface ITestServer : IAsyncDisposable
{
    Uri Uri { get; }
}

internal sealed record TestServer : ITestServer
{
    private readonly Func<ValueTask> _dispose;
    public Uri Uri { get; }

    public TestServer(Uri uri, Func<ValueTask> dispose)
    {
        _dispose = dispose;
        Uri = uri;
    }

    public ValueTask DisposeAsync()
    {
        return _dispose();
    }
}
