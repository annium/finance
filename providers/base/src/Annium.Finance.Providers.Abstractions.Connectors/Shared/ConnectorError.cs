namespace Annium.Finance.Providers.Abstractions.Connectors.Shared;

/// <summary>
/// An error reported by a connector, e.g. through <see cref="IConnectorBase.OnError"/>. Does not by itself change
/// the connector's <see cref="ConnectorStatus"/>.
/// </summary>
/// <param name="Message">A human-readable description of the error.</param>
public sealed record ConnectorError(string Message)
{
    /// <summary>
    /// Returns the error message.
    /// </summary>
    /// <returns>The value of <see cref="Message"/>.</returns>
    public override string ToString() => Message;
}
