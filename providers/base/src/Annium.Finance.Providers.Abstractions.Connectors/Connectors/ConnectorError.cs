namespace Annium.Finance.Providers.Abstractions.Connectors.Connectors;

public sealed record ConnectorError(string Message)
{
    public override string ToString() => Message;
}
