using System;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Shared;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Abstractions.Domain.Tests.Connectors;

/// <summary>
/// Pins that waiting for a connector to connect does not miss the moment it does.
/// </summary>
public class ConnectorExtensionsTests
{
    /// <summary>
    /// A connector that connects between the status being read and the handler being attached is still
    /// noticed. Reading first and subscribing after leaves a gap the transition can fall into, and a
    /// caller that falls into it waits for an event that has already happened - forever, on a connector
    /// that is connected.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task WhenConnectedAsync_ConnectingDuringTheCheck_IsNotMissed()
    {
        // arrange - connecting happens exactly once, as the status is read
        var connector = new ConnectsWhileBeingAskedConnector();

        // act
        var wait = connector.WhenConnectedAsync();

        // assert - bounded, because the failure being pinned is an unbounded wait
        var completed = await Task.WhenAny(
            wait,
            Task.Delay(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken)
        );

        (completed == wait).IsTrue("the wait must not outlive a connection that has already happened");
    }

    /// <summary>
    /// A connector that becomes connected as its status is read, raising the change before the reader has
    /// had a chance to subscribe.
    /// </summary>
    private sealed class ConnectsWhileBeingAskedConnector : IConnectorBase
    {
        /// <summary>Gets or sets a value indicating whether the status has been read once already.</summary>
        private bool Asked { get; set; }

        /// <summary>Gets or sets the status behind <see cref="Status"/>.</summary>
        private ConnectorStatus Current { get; set; } = ConnectorStatus.Connecting;

        /// <summary>
        /// Gets the status, connecting on the first read - after which the change has already been raised.
        /// </summary>
        public ConnectorStatus Status
        {
            get
            {
                if (Asked)
                    return Current;

                Asked = true;
                var answer = Current;
                Current = ConnectorStatus.Connected;
                OnStatusChanged(Current);

                return answer;
            }
        }

        /// <summary>Raised when the status changes.</summary>
        public event Action<ConnectorStatus> OnStatusChanged = delegate { };

        /// <summary>Never raised by this fake.</summary>
        public event Action<ConnectorError> OnError = delegate { };

        /// <summary>Does nothing.</summary>
        /// <returns>A completed task.</returns>
        public ValueTask DisposeAsync()
        {
            OnError(new ConnectorError(string.Empty));

            return ValueTask.CompletedTask;
        }
    }
}
