namespace Annium.Finance.Providers.Shared.ServerTime;

public interface IServerTimeSource
{
    long ServerTime { get; }
}
