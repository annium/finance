namespace Annium.Finance.Providers.Abstractions.Connectors.Shared;

public sealed record ConnectorError(string Message)
{
    public override string ToString() => Message;
}
