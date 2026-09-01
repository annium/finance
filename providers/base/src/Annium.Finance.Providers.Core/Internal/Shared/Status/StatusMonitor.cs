using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Annium.Finance.Providers.Abstractions.Connectors.Shared;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Linq;
using Annium.Logging;

namespace Annium.Finance.Providers.Core.Internal.Shared.Status;

/// <summary>
/// Default <see cref="IStatusMonitor"/> implementation. Tracks each registered target's status in a dictionary
/// and recomputes <see cref="Status"/> after every change: connected only when every target is connected,
/// disconnected only when every target is disconnected, connecting otherwise (including when there is a mix of
/// connected and disconnected targets).
/// </summary>
internal class StatusMonitor : IStatusMonitor, ILogSubject
{
    /// <summary>Gets the logger instance.</summary>
    public ILogger Logger { get; }

    /// <summary>Gets the overall connection status resolved from all registered targets.</summary>
    public ConnectorStatus Status { get; private set; }

    /// <summary>Raised whenever <see cref="Status"/> changes, with the new status.</summary>
    public event Action<ConnectorStatus> OnStatusChanged = delegate { };

    /// <summary>Surfaces errors reported by registered targets.</summary>
    public event Action<ConnectorError> OnError = delegate { };

    /// <summary>Synchronizes access to <see cref="_targets"/> and the status recomputation it triggers.</summary>
    private readonly Lock _locker = new();

    /// <summary>The current status of every registered target, keyed by target id.</summary>
    private readonly Dictionary<string, ConnectorStatus> _targets = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="StatusMonitor"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public StatusMonitor(ILogger logger)
    {
        Logger = logger;
        this.Trace("created");
    }

    /// <summary>
    /// Registers a new target with an initial status and recomputes <see cref="Status"/>.
    /// </summary>
    /// <param name="target">The id of the target to register.</param>
    /// <param name="status">The target's initial status.</param>
    /// <exception cref="InvalidOperationException">The target is already registered.</exception>
    public void Register(string target, ConnectorStatus status)
    {
        lock (_locker)
        {
            this.Trace<string>("add {target}", target);
            if (!_targets.TryAdd(target, status))
                throw new InvalidOperationException($"Target {target} is already registered");

            UpdateStatus();
        }
    }

    /// <summary>
    /// Removes a registered target and recomputes <see cref="Status"/>.
    /// </summary>
    /// <param name="target">The id of the target to unregister.</param>
    /// <exception cref="InvalidOperationException">The target is not registered.</exception>
    public void Unregister(string target)
    {
        lock (_locker)
        {
            this.Trace<string>("remove {target}", target);
            if (!_targets.Remove(target))
                throw new InvalidOperationException($"Target {target} is not registered");

            UpdateStatus();
        }
    }

    /// <summary>
    /// Updates a registered target's status and recomputes <see cref="Status"/>.
    /// </summary>
    /// <param name="target">The id of the target whose status changed.</param>
    /// <param name="status">The target's new status.</param>
    /// <exception cref="InvalidOperationException">The target is not registered.</exception>
    public void TrackStatus(string target, ConnectorStatus status)
    {
        lock (_locker)
        {
            this.Trace("{target} - {status}", target, status);
            if (!_targets.ContainsKey(target))
                throw new InvalidOperationException($"Target {target} is not registered");

            _targets[target] = status;

            UpdateStatus();
        }
    }

    /// <summary>
    /// Records an error reported by a target.
    /// </summary>
    /// <param name="target">The id of the target that reported the error.</param>
    /// <param name="error">The reported error.</param>
    public void Error(string target, ConnectorError error)
    {
        this.Trace("{target} - {error}", target, error);

        // this is the only path an error takes from a provider to a consumer: the connector base
        // subscribes here and re-raises it as its own OnError. Tracing it and stopping meant every
        // connector error was recorded where nobody reads it, and IConnectorBase.OnError never fired
        OnError(error);
    }

    /// <summary>
    /// Recomputes <see cref="Status"/> from the current target statuses and raises <see cref="OnStatusChanged"/>
    /// if it changed.
    /// </summary>
    private void UpdateStatus()
    {
        this.Trace<string>("state: {statuses}", GetStateDescription(_targets));
        var newStatus = ResolveStatus(_targets.Values);

        if (newStatus == Status)
        {
            this.Trace("same resolved status - {status}", newStatus);
            return;
        }

        this.Trace("update status {oldStatus} -> {newStatus}", Status, newStatus);
        OnStatusChanged(Status = newStatus);
    }

    /// <summary>
    /// Formats every target's status into a single human-readable string, for tracing.
    /// </summary>
    /// <param name="targets">The target statuses to describe.</param>
    /// <returns>A semicolon-separated "target - status" description of the given targets.</returns>
    private static string GetStateDescription(IReadOnlyDictionary<string, ConnectorStatus> targets) =>
        targets.Select(x => $"{x.Key} - {x.Value}").Join("; ");

    /// <summary>
    /// Resolves an overall status from a collection of target statuses: connected only if all are connected,
    /// disconnected only if all are disconnected, connecting otherwise.
    /// </summary>
    /// <param name="statuses">The target statuses to resolve an overall status from.</param>
    /// <returns>The resolved overall status.</returns>
    private static ConnectorStatus ResolveStatus(IReadOnlyCollection<ConnectorStatus> statuses)
    {
        if (statuses.Count == 0)
            return ConnectorStatus.Disconnected;

        var hasDisconnected = false;
        var hasConnecting = false;
        var hasConnected = false;

        foreach (var status in statuses)
        {
            hasDisconnected = hasDisconnected || status is ConnectorStatus.Disconnected;
            hasConnecting = hasConnecting || status is ConnectorStatus.Connecting;
            hasConnected = hasConnected || status is ConnectorStatus.Connected;
        }

        // if any connecting or both disconnected and connected in a moment - connecting
        if (hasConnecting || hasDisconnected && hasConnected)
            return ConnectorStatus.Connecting;

        return hasConnected ? ConnectorStatus.Connected : ConnectorStatus.Disconnected;
    }
}
