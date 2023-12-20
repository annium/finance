using System;
using System.Threading.Tasks;

namespace Annium.Finance.Providers.Abstractions.Connectors.Connectors;

public interface IConnectorBase : IAsyncDisposable
{
    ConnectorStatus Status { get; }
    event Action<ConnectorStatus> OnStatusChanged;
    event Action<ConnectorError> OnError;
    ValueTask InitAsync();
}
