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
            tcs.SetResult();
        }

        if (connector.Status is ConnectorStatus.Connected)
            return Task.CompletedTask;

        connector.OnStatusChanged += HandleStatusChanged;

        return tcs.Task;
    }
}
