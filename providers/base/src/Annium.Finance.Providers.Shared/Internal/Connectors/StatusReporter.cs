using System;
using System.Threading;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Shared.Connectors;
using Annium.Logging;

namespace Annium.Finance.Providers.Shared.Internal.Connectors;

internal class StatusReporter : IStatusReporter, ILogSubject
{
    public ILogger Logger { get; }
    private readonly StatusMonitor _monitor;
    private string _subjectId = string.Empty;

    public StatusReporter(StatusMonitor monitor, ILogger logger)
    {
        Logger = logger;
        _monitor = monitor;
    }

    public void Bind(object subject)
    {
        var currentSubjectId = Interlocked.CompareExchange(ref _subjectId, subject.GetFullId(), string.Empty);
        if (currentSubjectId != string.Empty)
            throw new InvalidOperationException($"{this.GetFullId()} is already bound to {currentSubjectId}");

        this.Trace<string, string>("{monitor} - register {subject}", _monitor.GetFullId(), _subjectId);
        _monitor.Register(_subjectId);
    }

    public void Connecting() => ReportStatus(ConnectorStatus.Connecting);

    public void Connected() => ReportStatus(ConnectorStatus.Connected);

    public void Disconnected() => ReportStatus(ConnectorStatus.Disconnected);

    private void ReportStatus(ConnectorStatus status)
    {
        if (_subjectId == string.Empty)
            throw new InvalidOperationException($"{this.GetFullId()} is not bound to any subject");

        this.Trace("{monitor} - set {subject} status to {status}", _monitor.GetFullId(), _subjectId, status);
        _monitor.TrackStatus(_subjectId, status);
    }
}
