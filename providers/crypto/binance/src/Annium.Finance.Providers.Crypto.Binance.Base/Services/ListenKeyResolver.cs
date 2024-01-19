using System;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using Annium.Finance.Providers.Crypto.Binance.Base.Connectors.Extensions;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.User.Domain;
using Annium.Finance.Providers.Shared.Connectors;
using Annium.Logging;
using Annium.Net.Http;
using Annium.Threading;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Services;

public sealed class ListenKeyResolver : IAsyncDisposable, ILogSubject
{
    public ILogger Logger { get; }
    public event Action<string> OnListenKeyFetched = delegate { };
    public event Action OnListenKeyReset = () => { };
    private readonly UserConfigBase _config;
    private readonly string _endpoint;
    private readonly IHttpRequestFactory _httpRequestFactory;
    private readonly SignatureService _signatureService;
    private readonly IStatusReporter _statusReporter;
    private readonly ISequentialTimer _timer;
    private readonly AsyncDisposableBox _disposable;
    private string _listenKey = string.Empty;

    public ListenKeyResolver(
        UserConfigBase config,
        string endpoint,
        IHttpRequestFactory httpRequestFactory,
        SignatureService signatureService,
        IStatusReporter statusReporter,
        ILogger logger
    )
    {
        Logger = logger;
        _config = config;
        _endpoint = endpoint;
        _httpRequestFactory = httpRequestFactory;
        _signatureService = signatureService;

        _statusReporter = statusReporter;
        _statusReporter.Bind(this);
        _statusReporter.Connecting();

        _disposable = Disposable.AsyncBox(logger);
        _disposable += _timer = Timers.Async(GetListenKeyAsync, 0, _config.ListenKey.FetchInterval, logger);
    }

    public async ValueTask DisposeAsync()
    {
        this.Trace("start");

        await _disposable.DisposeAsync();
        _statusReporter.Disconnected();

        this.Trace("done");
    }

    public void RequestNewListenKey()
    {
        this.Trace("start");

        _listenKey = string.Empty;
        _statusReporter.Connecting();
        _timer.Change(0, _config.ListenKey.FetchInterval);

        this.Trace("done");
    }

    private async ValueTask GetListenKeyAsync()
    {
        UserResult<ListenKey?>? result = null;
        try
        {
            this.Trace("start");

            // try get listen key - timer is not expected to be switched off at this moment
            result = await _httpRequestFactory
                .New(_config.HttpApi)
                .Post(_endpoint)
                .Key(_signatureService)
                .WithLogFrom(this, LogData.Headers | LogData.Response)
                .AsUserResultAsync<ListenKey>();

            // but here it can already be disposed
            if (_disposable.IsDisposed)
                return;

            // handle result
            if (result.IsSuccess)
                HandleSuccessfulResult(result.Data);
            else
                HandleFailure(result.Status, result.Message);

            this.Trace("done");
        }
        catch (Exception e)
        {
            this.Trace("handle exception - start");

            HandleFailure(
                UserOperationStatus.UnknownError,
                result is not null
                    ? $"Listen key processing error for response '{result.Data}': {e}"
                    : $"Listen key processing error: {e}"
            );

            this.Trace("handle exception - done");
        }
    }

    private void HandleSuccessfulResult(ListenKey listenKey)
    {
        this.Trace("start");

        // if not previously connected yet - set listen key, fire fetched and change timer confirmation mode
        if (string.IsNullOrWhiteSpace(_listenKey))
        {
            this.Trace("listen key acquired");

            _listenKey = listenKey.Value;
            OnListenKeyFetched(_listenKey);

            _statusReporter.Connected();

            _timer.Change(_config.ListenKey.ConfirmInterval, _config.ListenKey.ConfirmInterval);

            this.Trace("done");

            return;
        }

        // if previously connected with same listen key - noop
        if (listenKey.Value == _listenKey)
        {
            this.Trace("listen key confirmed");

            return;
        }

        // previously connected, but listen key changed - update listen key fire reset and fetched events
        this.Trace("listen key changed");

        _listenKey = string.Empty;
        _statusReporter.Connecting();

        OnListenKeyReset();

        this.Trace("done");
    }

    private void HandleFailure(UserOperationStatus status, string message)
    {
        this.Trace("start");

        // if was not connected - it's 2+ fetch attempt - just continue retrying
        if (string.IsNullOrWhiteSpace(_listenKey))
        {
            this.Trace("stop - listen key fetch failed");
            _statusReporter.Connecting();
            _statusReporter.Error(new ConnectorError($"{status}: {message}"));

            OnListenKeyReset();

            this.Trace("done");

            return;
        }

        // if was connected - fire reset and change timer to fetch mode
        this.Trace("stop - listen key confirmation failed");

        _listenKey = string.Empty;
        _statusReporter.Connecting();
        _statusReporter.Error(new ConnectorError($"{status}: {message}"));

        OnListenKeyReset();

        _timer.Change(_config.ListenKey.FetchInterval, _config.ListenKey.FetchInterval);

        this.Trace("done");
    }
}
