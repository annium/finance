using System;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Finance.Providers.Core.Shared.TimeSync;
using Annium.Logging;

namespace Annium.Finance.Providers.Core.Internal.Shared.TimeSync;

internal class ServerTimeTracker : IServerTimeTracker, IDisposable, ILogSubject
{
    public ILogger Logger { get; }
    public long ServerTime => _provider.ServerTime;
    private readonly IServerTimeProvider _provider;
    private readonly IStatusReporter _statusReporter;

    public ServerTimeTracker(IServerTimeProvider provider, IStatusReporter statusReporter, ILogger logger)
    {
        Logger = logger;
        _provider = provider;
        _statusReporter = statusReporter;

        _statusReporter.Bind(this);
        _statusReporter.Connecting();

        _provider.OnStateChanged += HandleProviderStateChanged;
    }

    public void Dispose()
    {
        this.Trace("start");

        _provider.OnStateChanged -= HandleProviderStateChanged;

        _statusReporter.Disconnected();

        this.Trace("done");
    }

    private void HandleProviderStateChanged(bool state)
    {
        if (state)
            _statusReporter.Connected();
        else
            _statusReporter.Connecting();
    }
}
