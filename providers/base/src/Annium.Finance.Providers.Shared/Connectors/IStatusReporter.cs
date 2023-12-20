using Annium.Finance.Providers.Abstractions.Connectors.Connectors;

namespace Annium.Finance.Providers.Shared.Connectors;

public interface IStatusReporter
{
    void Bind(object component);
    void Unbind();
    void Connecting();
    void Connected();
    void Disconnected();
    void Error(ConnectorError error);
}
