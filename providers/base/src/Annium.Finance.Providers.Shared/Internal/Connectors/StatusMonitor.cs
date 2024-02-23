using System;
using System.Collections.Generic;
using System.Linq;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Shared.Connectors;
using Annium.Linq;
using Annium.Logging;

namespace Annium.Finance.Providers.Shared.Internal.Connectors;

internal class StatusMonitor : IStatusMonitor, IDisposable, ILogSubject
{
    public ILogger Logger { get; }
    public ConnectorStatus Status { get; private set; }
    public event Action<ConnectorStatus> OnStatusChanged = delegate { };
    public event Action<ConnectorError> OnError = delegate { };
    private readonly object _locker = new();
    private readonly Dictionary<string, ConnectorStatus> _targets = new();

    public StatusMonitor(ILogger logger)
    {
        Logger = logger;
        this.Trace("start");
    }

    public void Dispose()
    {
        this.Trace("start");
    }

    public void Register(string target, ConnectorStatus status)
    {
        lock (_locker)
        {
            this.Trace<string>("add {target}", target);
            _targets.Add(target, ConnectorStatus.Disconnected);

            UpdateStatus();
        }
    }

    public void Unregister(string target)
    {
        lock (_locker)
        {
            this.Trace<string>("remove {target}", target);
            if (!_targets.Remove(target))
                throw new InvalidOperationException($"Target {target} was not registered");

            UpdateStatus();
        }
    }

    public void TrackStatus(string target, ConnectorStatus status)
    {
        lock (_locker)
        {
            this.Trace("{target} - {status}", target, status);
            _targets[target] = status;

            UpdateStatus();
        }
    }

    public void Error(string target, ConnectorError error)
    {
        this.Trace("{target} - {error}", target, error);
    }

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

    private static string GetStateDescription(IReadOnlyDictionary<string, ConnectorStatus> targets) =>
        targets.Select(x => $"{x.Key} - {x.Value}").Join("; ");

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
