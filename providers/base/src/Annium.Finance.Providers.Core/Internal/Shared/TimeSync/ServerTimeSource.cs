using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Finance.Providers.Core.Shared.TimeSync;
using Annium.Logging;
using Annium.Threading;
using NodaTime;

namespace Annium.Finance.Providers.Core.Internal.Shared.TimeSync;

internal class ServerTimeSource : IServerTimeSource, IDisposable, ILogSubject
{
    public ILogger Logger { get; }
    public long ServerTime
    {
        get => field + _watch.ElapsedMilliseconds;
        private set;
    } = SystemClock.Instance.GetCurrentInstant().ToUnixTimeMilliseconds();
    private readonly IServerTimeProvider _provider;
    private readonly ServerTimeProviderConfig _config;
    private readonly IStatusReporter _statusReporter;
    private readonly Stopwatch _watch = new();
    private readonly ISequentialTimer _timer;
    private readonly CancellationTokenSource _cts = new();
    private Mode _mode = Mode.Load;

    public ServerTimeSource(
        IServerTimeProvider provider,
        ServerTimeProviderConfig config,
        IStatusReporter statusReporter,
        ILogger logger
    )
    {
        Logger = logger;
        _provider = provider;
        _config = config;
        _statusReporter = statusReporter;

        _statusReporter.Bind(this);
        _statusReporter.Connecting();

        _watch.Start();
        _timer = Timers.Async(RefreshAsync, 0, _config.LoadInterval, logger);
    }

    public void Dispose()
    {
        this.Trace("start");

        _watch.Stop();
        _timer.Dispose();
        _cts.Cancel();
        _cts.Dispose();

        _statusReporter.Disconnected();

        this.Trace("done");
    }

    private async ValueTask RefreshAsync()
    {
        this.Trace("start");

        this.Trace("load server time");
        var result = await _provider.LoadAsync(_cts.Token);

        if (!result.IsSuccess)
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
        ServerTime = result.Data;
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
        Confirm,
    }
}
