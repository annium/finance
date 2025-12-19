using System;
using Annium.Finance.Providers.Abstractions.Connectors.Shared;

namespace Annium.Finance.Providers.Core.Shared.Status;

public interface IStatusMonitor
{
    ConnectorStatus Status { get; }
    event Action<ConnectorStatus> OnStatusChanged;
    event Action<ConnectorError> OnError;
}
