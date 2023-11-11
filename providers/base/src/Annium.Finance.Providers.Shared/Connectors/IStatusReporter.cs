namespace Annium.Finance.Providers.Shared.Connectors;

public interface IStatusReporter
{
    void Bind(object component);
    void Unbind();
    void Connecting();
    void Connected();
    void Disconnected();
}
