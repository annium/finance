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
    private string _target = string.Empty;

    public StatusReporter(StatusMonitor monitor, ILogger logger)
    {
        Logger = logger;
        _monitor = monitor;
        this.Trace<string>("reports to {monitor}", monitor.GetFullId());
    }

    public void Bind<T>(T component, ConnectorStatus status = ConnectorStatus.Disconnected)
    {
        var target = component.GetFullId();
        var current = Interlocked.CompareExchange(ref _target, target, string.Empty);
        if (current != string.Empty)
            throw new InvalidOperationException($"{this.GetFullId()} is already bound to {current}");

        this.Trace("register {target} in {status} status", target, status);
        _monitor.Register(target, status);
    }

    public void Unbind()
    {
        var target = Interlocked.Exchange(ref _target, string.Empty);
        if (target == string.Empty)
            throw new InvalidOperationException($"{this.GetFullId()} is already unbound");

        this.Trace<string>("unregister {target}", target);
        _monitor.Unregister(target);
    }

    public void Connecting() => ReportStatus(ConnectorStatus.Connecting);

    public void Connected() => ReportStatus(ConnectorStatus.Connected);

    public void Disconnected() => ReportStatus(ConnectorStatus.Disconnected);

    public void Error(ConnectorError error)
    {
        var target = GetTarget();

        this.Trace("report {target} error {error}", target, error);
        _monitor.Error(target, error);
    }

    private void ReportStatus(ConnectorStatus status)
    {
        var target = GetTarget();

        this.Trace("set {target} status to {status}", target, status);
        _monitor.TrackStatus(target, status);
    }

    private string GetTarget()
    {
        var target = _target;

        return target == string.Empty
            ? throw new InvalidOperationException($"{this.GetFullId()} is not bound to any target")
            : target;
    }
}
