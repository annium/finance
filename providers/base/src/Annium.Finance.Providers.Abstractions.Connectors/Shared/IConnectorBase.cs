using System;

namespace Annium.Finance.Providers.Abstractions.Connectors.Shared;

/// <summary>
/// Common contract shared by market and user connectors: connection status tracking and error/status
/// notifications, plus async disposal to tear the connection down.
/// </summary>
public interface IConnectorBase : IAsyncDisposable
{
    /// <summary>Gets the current connection status of the connector.</summary>
    ConnectorStatus Status { get; }

    /// <summary>Raised whenever <see cref="Status"/> changes, with the new status.</summary>
    event Action<ConnectorStatus> OnStatusChanged;

    /// <summary>
    /// Raised when the connector encounters an error, e.g. a failed request or an unexpected disconnect. Does not
    /// necessarily imply a status change.
    /// </summary>
    event Action<ConnectorError> OnError;
}
