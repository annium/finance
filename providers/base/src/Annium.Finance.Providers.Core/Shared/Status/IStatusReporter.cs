using Annium.Finance.Providers.Abstractions.Connectors.Shared;

namespace Annium.Finance.Providers.Core.Shared.Status;

/// <summary>
/// Lets a single component report its connection status and errors to an
/// <see cref="Annium.Finance.Providers.Core.Internal.Shared.Status.StatusMonitor"/>, once bound to it via
/// <see cref="Bind{T}"/>.
/// </summary>
public interface IStatusReporter
{
    /// <summary>
    /// Binds this reporter to a component, registering it with the underlying monitor under an initial status.
    /// Must be called once before any of <see cref="Connecting"/>, <see cref="Connected"/>,
    /// <see cref="Disconnected"/>, or <see cref="Error"/> can be used.
    /// </summary>
    /// <typeparam name="T">The type of the component being bound.</typeparam>
    /// <param name="component">The component to bind this reporter to.</param>
    /// <param name="status">The initial status to report for the component.</param>
    void Bind<T>(T component, ConnectorStatus status = ConnectorStatus.Disconnected);

    /// <summary>
    /// Unbinds this reporter, unregistering its component from the underlying monitor.
    /// </summary>
    void Unbind();

    /// <summary>Reports the bound component as connecting.</summary>
    void Connecting();

    /// <summary>Reports the bound component as connected.</summary>
    void Connected();

    /// <summary>Reports the bound component as disconnected.</summary>
    void Disconnected();

    /// <summary>
    /// Reports an error for the bound component.
    /// </summary>
    /// <param name="error">The error to report.</param>
    void Error(ConnectorError error);
}
