using System;
using System.Collections.Generic;
using System.Linq;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Shared.Connectors;
using Annium.Linq;
using Annium.Logging;

namespace Annium.Finance.Providers.Shared.Internal.Connectors;

internal class StatusMonitor : IStatusMonitor, ILogSubject
{
    public ILogger Logger { get; }
    public event Action<ConnectorStatus> OnStatusChanged = delegate { };
    private readonly object _locker = new();
    private readonly Dictionary<string, ConnectorStatus> _subjects = new();
    private ConnectorStatus _status;

    public StatusMonitor(ILogger logger)
    {
        Logger = logger;
    }

    public void Register(string subjectId)
    {
        lock (_locker)
        {
            _subjects.Add(subjectId, ConnectorStatus.Disconnected);
        }
    }

    public void TrackStatus(string subjectId, ConnectorStatus status)
    {
        lock (_locker)
        {
            this.Trace("{subject} - {status}", subjectId, status);

            this.Trace<string>("current state: {statuses}", GetStateDescription(_subjects));
            _subjects[subjectId] = status;
            this.Trace<string>("new state: {statuses}", GetStateDescription(_subjects));

            var newStatus = ResolveStatus(_subjects.Values);

            if (newStatus == _status)
            {
                this.Trace("same resolved status - {status}", newStatus);
                return;
            }

            this.Trace("update status {oldStatus} -> {newStatus}", _status, newStatus);
            OnStatusChanged(_status = newStatus);
        }
    }

    private static string GetStateDescription(IReadOnlyDictionary<string, ConnectorStatus> subjects) =>
        subjects.Select(x => $"{x.Key} - {x.Value}").Join("; ");

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
