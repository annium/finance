using System;
using Annium.Finance.Providers.Abstractions.Connectors.Shared;

namespace Annium.Finance.Providers.Core.Shared.Status;

/// <summary>
/// Aggregates the connection status of a set of named targets (registered via
/// <see cref="Annium.Finance.Providers.Core.Internal.Shared.Status.StatusMonitor.Register"/>) into a single
/// overall <see cref="Status"/>: connected only if every target is connected, disconnected only if every target
/// is disconnected, and connecting otherwise.
/// </summary>
public interface IStatusMonitor
{
    /// <summary>Gets the overall connection status resolved from all registered targets.</summary>
    ConnectorStatus Status { get; }

    /// <summary>Raised whenever <see cref="Status"/> changes, with the new status.</summary>
    event Action<ConnectorStatus> OnStatusChanged;

    /// <summary>Surfaces errors reported by registered targets.</summary>
    event Action<ConnectorError> OnError;
}
