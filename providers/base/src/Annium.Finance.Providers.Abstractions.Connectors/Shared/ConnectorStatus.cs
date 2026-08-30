namespace Annium.Finance.Providers.Abstractions.Connectors.Shared;

/// <summary>
/// The connection state of an <see cref="IConnectorBase"/>.
/// </summary>
public enum ConnectorStatus
{
    /// <summary>The connector has no active connection to the provider.</summary>
    Disconnected,

    /// <summary>The connector is establishing or re-establishing its connection and (re)loading its data.</summary>
    Connecting,

    /// <summary>The connector is connected and its data is up to date.</summary>
    Connected,
}
