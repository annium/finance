using System.Threading.Tasks;

namespace Annium.Finance.Providers.Abstractions.Connectors.Shared;

/// <summary>
/// Extension methods for <see cref="IConnectorBase"/>.
/// </summary>
public static class ConnectorExtensions
{
    /// <summary>
    /// Waits until the connector reaches the <see cref="ConnectorStatus.Connected"/> status. Completes
    /// immediately if the connector is already connected.
    /// </summary>
    /// <param name="connector">The connector to wait on.</param>
    /// <returns>A task that completes once the connector becomes connected.</returns>
    public static Task WhenConnectedAsync(this IConnectorBase connector)
    {
        var tcs = new TaskCompletionSource();

        void HandleStatusChanged(ConnectorStatus status)
        {
            if (status is not ConnectorStatus.Connected)
                return;

            connector.OnStatusChanged -= HandleStatusChanged;
            tcs.TrySetResult();
        }

        // subscribe first, then look: reading the status first leaves a gap between the two that the
        // transition can fall into, seen by neither - and a caller that falls into it waits for an event
        // that has already happened, on a connector that is connected
        connector.OnStatusChanged += HandleStatusChanged;

        if (connector.Status is ConnectorStatus.Connected)
        {
            connector.OnStatusChanged -= HandleStatusChanged;

            return Task.CompletedTask;
        }

        return tcs.Task;
    }
}
