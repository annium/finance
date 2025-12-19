using System;
using Annium.Finance.Providers.Abstractions.Connectors.Shared;

namespace Annium.Finance.Providers.Shared.Connectors;

public interface IStatusMonitor
{
    ConnectorStatus Status { get; }
    event Action<ConnectorStatus> OnStatusChanged;
    event Action<ConnectorError> OnError;
}
