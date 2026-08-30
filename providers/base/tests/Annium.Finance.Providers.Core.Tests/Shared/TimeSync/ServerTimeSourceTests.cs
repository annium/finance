using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Market.Operations;
using Annium.Finance.Providers.Core.Internal.Shared.TimeSync;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Finance.Providers.Core.Shared.TimeSync;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Core.Tests.Shared.TimeSync;

/// <summary>
/// Pins what disposing the server time source does to a refresh that is still in flight.
/// </summary>
public class ServerTimeSourceTests : ProvidersTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServerTimeSourceTests"/> class.
    /// </summary>
    /// <param name="outputHelper">The xUnit output helper used to capture test logs.</param>
    public ServerTimeSourceTests(ITestOutputHelper outputHelper)
        : base(outputHelper) { }

    /// <summary>
    /// Disposal cancels the refresh before waiting for it. Disposing the timer drains whatever callback is
    /// running, and the cancellation is what lets that callback end; issued afterwards, the wait runs its
    /// whole budget with the load not yet told to stop - and a load outliving that budget then reports
    /// against a reporter disposal has already unbound.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task Dispose_CancelsTheRefreshBeforeWaitingForIt()
    {
        // arrange - a load that ends only when its token says so
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = new GatedServerTimeProvider(started);
        var source = new ServerTimeSource(
            provider,
            new ServerTimeProviderConfig(1, 10_000),
            Get<IStatusReporter>(),
            Logger
        );

        // act
        await started.Task;
        var watch = Stopwatch.StartNew();
        // VSTHRD103: the subject is IDisposable; disposing it synchronously is the behaviour under test
#pragma warning disable VSTHRD103
        source.Dispose();
#pragma warning restore VSTHRD103
        watch.Stop();

        // assert - the drain budget is seconds; cancelled first, this returns in a fraction of it
        (watch.ElapsedMilliseconds < 2000).IsTrue($"disposal took {watch.ElapsedMilliseconds}ms");
        provider.WasCanceled.IsTrue("the in-flight load must be cancelled, not merely waited out");
    }

    /// <summary>
    /// A server time provider whose load blocks until its token is cancelled.
    /// </summary>
    private sealed class GatedServerTimeProvider : IServerTimeProvider
    {
        /// <summary>Signalled once the first load has begun.</summary>
        private readonly TaskCompletionSource _started;

        /// <summary>
        /// Initializes a new instance of the <see cref="GatedServerTimeProvider"/> class.
        /// </summary>
        /// <param name="started">Signalled once the first load has begun.</param>
        public GatedServerTimeProvider(TaskCompletionSource started)
        {
            _started = started;
        }

        /// <summary>
        /// Gets a value indicating whether the load ended because its token was cancelled.
        /// </summary>
        public bool WasCanceled { get; private set; }

        /// <summary>
        /// Blocks until the given token is cancelled, then reports failure.
        /// </summary>
        /// <param name="ct">The cancellation token to observe.</param>
        /// <returns>A failing result, once cancelled.</returns>
        public async Task<MarketResult<long>> LoadAsync(CancellationToken ct)
        {
            _started.TrySetResult();

            try
            {
                await Task.Delay(Timeout.Infinite, ct);
            }
            catch (OperationCanceledException)
            {
                WasCanceled = true;
            }

            return MarketResult.New<long>(MarketOperationStatus.NetworkError, 0);
        }
    }
}
