namespace Annium.Finance.Providers.Shared.Connectors;

public interface IStatusReporter
{
    void Bind(object subject);
    void Connecting();
    void Connected();
    void Disconnected();
}
