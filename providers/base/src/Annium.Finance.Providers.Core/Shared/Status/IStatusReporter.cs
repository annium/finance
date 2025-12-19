using Annium.Finance.Providers.Abstractions.Connectors.Shared;

namespace Annium.Finance.Providers.Core.Shared.Status;

public interface IStatusReporter
{
    void Bind<T>(T component, ConnectorStatus status = ConnectorStatus.Disconnected);
    void Unbind();
    void Connecting();
    void Connected();
    void Disconnected();
    void Error(ConnectorError error);
}
