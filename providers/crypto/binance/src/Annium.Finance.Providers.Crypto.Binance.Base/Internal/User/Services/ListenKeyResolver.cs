using System;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Shared;
using Annium.Finance.Providers.Abstractions.Domain.User.Operations;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Finance.Providers.Crypto.Binance.Base.Internal.User.HttpExtensions;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared.HttpExtensions;
using Annium.Finance.Providers.Crypto.Binance.Base.User;
using Annium.Finance.Providers.Crypto.Binance.Base.User.Contracts.Domain;
using Annium.Finance.Providers.Crypto.Binance.Base.User.Services;
using Annium.Logging;
using Annium.Net.Http;
using Annium.Threading;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Internal.User.Services;

/// <summary>
/// Keeps a Binance user data stream listen key alive by periodically requesting a new one until it succeeds, then
/// switching to periodic PUT (keep-alive) confirmations, and re-fetching whenever a confirmation fails or the key changes.
/// </summary>
internal class ListenKeyResolver : IListenKeyResolver, ILogSubject
{
    /// <summary>Gets the logger used to trace listen key fetch and keep-alive activity.</summary>
    public ILogger Logger { get; }

    /// <summary>Raised when a listen key is fetched or confirmed for the first time since a reset.</summary>
    public event Action<string> OnListenKeyFetched = delegate { };

    /// <summary>Raised when the current listen key is invalidated and must be re-fetched.</summary>
    public event Action OnListenKeyReset = () => { };

    /// <summary>The user configuration providing the HTTP API and listen key fetch/confirm intervals.</summary>
    private readonly UserConfigBase _config;

    /// <summary>The relative path of the listen key endpoint.</summary>
    private readonly string _endpoint;

    /// <summary>The factory used to build listen key HTTP requests.</summary>
    private readonly IHttpRequestFactory _httpRequestFactory;

    /// <summary>The service used to sign the listen key request.</summary>
    private readonly ISignatureService _signatureService;

    /// <summary>The reporter used to publish connection status changes.</summary>
    private readonly IStatusReporter _statusReporter;

    /// <summary>The timer driving listen key fetch and keep-alive requests.</summary>
    private readonly ISequentialTimer _timer;

    /// <summary>The disposable box tearing down the timer on dispose.</summary>
    private readonly AsyncDisposableBox _disposable;

    /// <summary>The currently confirmed listen key, or empty if none has been confirmed yet.</summary>
    private string _listenKey = string.Empty;

    /// <summary>Initializes a new instance of the <see cref="ListenKeyResolver"/> class and starts the listen key fetch timer.</summary>
    /// <param name="config">The user configuration providing the HTTP API and listen key fetch/confirm intervals.</param>
    /// <param name="endpoint">The relative path of the listen key endpoint.</param>
    /// <param name="httpRequestFactory">The factory used to build listen key HTTP requests.</param>
    /// <param name="signatureService">The service used to sign the listen key request.</param>
    /// <param name="statusReporter">The reporter used to publish connection status changes.</param>
    /// <param name="logger">The logger to trace listen key activity with.</param>
    public ListenKeyResolver(
        UserConfigBase config,
        string endpoint,
        IHttpRequestFactory httpRequestFactory,
        ISignatureService signatureService,
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
        // a timer is both IDisposable and IAsyncDisposable now, so the box's operators are ambiguous
        // without saying which teardown is wanted - the async one, since the box is async
        _timer = Timers.Async(GetListenKeyAsync, 0, _config.ListenKey.FetchInterval, logger);
        _disposable += (IAsyncDisposable)_timer;
    }

    /// <summary>Stops the fetch/keep-alive timer and reports the connector as disconnected.</summary>
    /// <returns>A value task representing the asynchronous teardown.</returns>
    public async ValueTask DisposeAsync()
    {
        this.Trace("start");

        await _disposable.DisposeAsync();
        _statusReporter.Disconnected();

        // and stop counting: a disposed component is gone, not disconnected. Left registered, it sits
        // in the monitor as a disconnected target beside the live ones, and the connector can never
        // report itself connected again for as long as it lives
        _statusReporter.Unbind();

        this.Trace("done");
    }

    /// <summary>Discards the current listen key, if any, and restarts the fetch timer to acquire a new one immediately.</summary>
    public void RequestNewListenKey()
    {
        this.Trace("start");

        _listenKey = string.Empty;
        _statusReporter.Connecting();
        _timer.Change(0, _config.ListenKey.FetchInterval);

        this.Trace("done");
    }

    /// <summary>Requests or confirms the listen key from Binance's signed listen key endpoint and handles the outcome.</summary>
    /// <returns>A value task representing the asynchronous request.</returns>
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
                .WithLogFromWithHeaders(this, LogData.Headers | LogData.Response)
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

    /// <summary>Processes a successfully returned listen key: confirms the first key, keeps an unchanged key alive, or resets on change.</summary>
    /// <param name="listenKey">The listen key returned by Binance.</param>
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

    /// <summary>Reports a listen key fetch or keep-alive failure and, if a key was previously confirmed, resets it and switches the timer back to fetch mode.</summary>
    /// <param name="status">The status describing why the request failed.</param>
    /// <param name="message">The failure message to report.</param>
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
