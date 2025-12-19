using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Market.Operations;
using Annium.Logging;
using Annium.Threading;
using NodaTime;

namespace Annium.Finance.Providers.Shared.ServerTime;

public abstract class ServerTimeProviderBase : IServerTimeProvider, IDisposable, ILogSubject
{
    public ILogger Logger { get; }
    public long ServerTime
    {
        get => field + _watch.ElapsedMilliseconds;
        private set;
    } = SystemClock.Instance.GetCurrentInstant().ToUnixTimeMilliseconds();
    public event Action<bool> OnStateChanged = delegate { };
    private readonly ServerTimeProviderConfig _config;
    private readonly Stopwatch _watch = new();
    private readonly ISequentialTimer _timer;
    private readonly CancellationTokenSource _cts = new();
    private Mode _mode = Mode.Load;

    protected ServerTimeProviderBase(ServerTimeProviderConfig config, ILogger logger)
    {
        Logger = logger;
        _config = config;
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

        this.Trace("done");
    }

    protected abstract Task<MarketResult<long>> LoadAsync(CancellationToken ct);

    private async ValueTask RefreshAsync()
    {
        this.Trace("start");

        this.Trace("load server time");
        var result = await LoadAsync(_cts.Token);

        if (!result.IsSuccess)
        {
            this.Trace("server time load failed ({result})", result);

            OnStateChanged(false);

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

        OnStateChanged(true);

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
