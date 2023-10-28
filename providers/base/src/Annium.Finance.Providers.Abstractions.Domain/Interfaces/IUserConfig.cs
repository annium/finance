namespace Annium.Finance.Providers.Abstractions.Domain.Interfaces;

public interface IUserConfig : IConnectorConfig
{
    string Key { get; }
    string Secret { get; }
}
