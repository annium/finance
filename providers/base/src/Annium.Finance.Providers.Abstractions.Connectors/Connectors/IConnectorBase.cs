using System;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;

namespace Annium.Finance.Providers.Abstractions.Connectors.Connectors;

public interface IConnectorBase<TConfig>
    where TConfig : IConnectorConfig
{
    event Action<ConnectorStatus> OnStatusChanged;
    TConfig Config { get; }
    ValueTask InitAsync(TConfig config);
}
