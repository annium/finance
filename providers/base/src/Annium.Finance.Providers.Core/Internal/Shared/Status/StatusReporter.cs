using System;
using System.Threading;
using Annium.Finance.Providers.Abstractions.Connectors.Shared;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Logging;

namespace Annium.Finance.Providers.Core.Internal.Shared.Status;

/// <summary>
/// Default <see cref="IStatusReporter"/> implementation. Relays a single bound component's status and error
/// reports to a shared <see cref="StatusMonitor"/>, identifying the component by its full id.
/// </summary>
internal class StatusReporter : IStatusReporter, ILogSubject
{
    /// <summary>Gets the logger instance.</summary>
    public ILogger Logger { get; }

    /// <summary>The shared monitor this reporter relays status and errors to.</summary>
    private readonly StatusMonitor _monitor;

    /// <summary>The full id of the currently bound component, or empty if not bound.</summary>
    private string _target = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="StatusReporter"/> class.
    /// </summary>
    /// <param name="monitor">The shared monitor this reporter relays status and errors to.</param>
    /// <param name="logger">The logger instance.</param>
    public StatusReporter(StatusMonitor monitor, ILogger logger)
    {
        Logger = logger;
        _monitor = monitor;
        this.Trace<string>("reports to {monitor}", monitor.GetFullId());
    }

    /// <summary>
    /// Binds this reporter to a component, registering it with the underlying monitor under an initial status.
    /// </summary>
    /// <typeparam name="T">The type of the component being bound.</typeparam>
    /// <param name="component">The component to bind this reporter to.</param>
    /// <param name="status">The initial status to report for the component.</param>
    /// <exception cref="InvalidOperationException">This reporter is already bound to a component.</exception>
    public void Bind<T>(T component, ConnectorStatus status = ConnectorStatus.Disconnected)
    {
        var target = component.GetFullId();
        var current = Interlocked.CompareExchange(ref _target, target, string.Empty);
        if (current != string.Empty)
            throw new InvalidOperationException($"{this.GetFullId()} is already bound to {current}");

        this.Trace("register {target} in {status} status", target, status);
        _monitor.Register(target, status);
    }

    /// <summary>
    /// Unbinds this reporter, unregistering its component from the underlying monitor.
    /// </summary>
    /// <exception cref="InvalidOperationException">This reporter is not currently bound.</exception>
    public void Unbind()
    {
        var target = Interlocked.Exchange(ref _target, string.Empty);
        if (target == string.Empty)
            throw new InvalidOperationException($"{this.GetFullId()} is already unbound");

        this.Trace<string>("unregister {target}", target);
        _monitor.Unregister(target);
    }

    /// <summary>Reports the bound component as connecting.</summary>
    public void Connecting() => ReportStatus(ConnectorStatus.Connecting);

    /// <summary>Reports the bound component as connected.</summary>
    public void Connected() => ReportStatus(ConnectorStatus.Connected);

    /// <summary>Reports the bound component as disconnected.</summary>
    public void Disconnected() => ReportStatus(ConnectorStatus.Disconnected);

    /// <summary>
    /// Reports an error for the bound component.
    /// </summary>
    /// <param name="error">The error to report.</param>
    /// <exception cref="InvalidOperationException">This reporter is not currently bound.</exception>
    public void Error(ConnectorError error)
    {
        var target = GetTarget();

        this.Trace("report {target} error {error}", target, error);
        _monitor.Error(target, error);
    }

    /// <summary>
    /// Reports the given status for the bound component to the underlying monitor.
    /// </summary>
    /// <param name="status">The status to report.</param>
    private void ReportStatus(ConnectorStatus status)
    {
        var target = GetTarget();

        this.Trace("set {target} status to {status}", target, status);
        _monitor.TrackStatus(target, status);
    }

    /// <summary>
    /// Gets the full id of the currently bound component.
    /// </summary>
    /// <returns>The full id of the currently bound component.</returns>
    /// <exception cref="InvalidOperationException">This reporter is not currently bound.</exception>
    private string GetTarget()
    {
        var target = _target;

        return target == string.Empty
            ? throw new InvalidOperationException($"{this.GetFullId()} is not bound to any target")
            : target;
    }
}
