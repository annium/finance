using System;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;

namespace Annium.Finance.Providers.Shared.Connectors;

public interface IStatusMonitor
{
    event Action<ConnectorStatus> OnStatusChanged;
}
