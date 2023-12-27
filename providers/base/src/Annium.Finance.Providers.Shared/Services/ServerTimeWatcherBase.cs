using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using Annium.Finance.Providers.Shared.Connectors;
using Annium.Logging;
using Annium.Threading;
using NodaTime;

namespace Annium.Finance.Providers.Shared.Services;

public abstract class ServerTimeWatcherBase : IDisposable, ILogSubject
{
    public ILogger Logger { get; }
    public long ServerTime => _serverTime + _watch.ElapsedMilliseconds;
    private readonly ServerTimeWatcherConfig _config;
    private readonly IStatusReporter _statusReporter;
    private readonly Stopwatch _watch = new();
    private readonly IAsyncTimer _timer;
    private readonly CancellationTokenSource _cts = new();
    private long _serverTime = SystemClock.Instance.GetCurrentInstant().ToUnixTimeMilliseconds();
    private Mode _mode = Mode.Load;

    protected ServerTimeWatcherBase(ServerTimeWatcherConfig config, IStatusReporter statusReporter, ILogger logger)
    {
        Logger = logger;
        _config = config;
        _statusReporter = statusReporter;
        _watch.Start();

        _statusReporter.Bind(this);
        _statusReporter.Connecting();

        _timer = Timers.Async(RefreshAsync, 0, _config.LoadInterval);
    }

    public void Dispose()
    {
        this.Trace("start");

        _watch.Stop();
        _timer.Dispose();
        _cts.Cancel();
        _cts.Dispose();

        _statusReporter.Disconnected();

        this.Trace("start");
    }

    protected abstract Task<MarketResult<long>> LoadAsync(CancellationToken ct);

    private async ValueTask RefreshAsync()
    {
        this.Trace("start");

        this.Trace("load server time");
        var result = await LoadAsync(_cts.Token);

        if (result.IsFailure)
        {
            this.Trace("server time load failed ({result})", result);

            _statusReporter.Connecting();

            if (_mode is Mode.Confirm)
            {
                this.Trace("switch to load mode");
                _mode = Mode.Load;
                _timer.Change(_config.LoadInterval, _config.LoadInterval);
            }

            this.Trace("end");

            return;
        }

        this.Trace("update server time and restart watch");
        _serverTime = result.Data;
        _watch.Restart();

        _statusReporter.Connected();

        if (_mode is Mode.Load)
        {
            this.Trace("switch to confirm mode");
            _mode = Mode.Confirm;
            _timer.Change(_config.ConfirmInterval, _config.ConfirmInterval);
        }

        this.Trace("end");
    }

    private enum Mode
    {
        Load,
        Confirm
    }
}
