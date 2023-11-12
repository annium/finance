using System;
using System.Threading.Tasks;

namespace Annium.Finance.Providers.Abstractions.Connectors.Connectors;

public interface IConnectorBase : IAsyncDisposable
{
    event Action<ConnectorStatus> OnStatusChanged;
    ValueTask InitAsync();
}
