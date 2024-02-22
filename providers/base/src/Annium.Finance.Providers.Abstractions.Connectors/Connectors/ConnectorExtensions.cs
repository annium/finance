using System.Threading.Tasks;

namespace Annium.Finance.Providers.Abstractions.Connectors.Connectors;

public static class ConnectorExtensions
{
    public static Task WhenConnected(this IConnectorBase connector)
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
