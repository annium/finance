namespace Annium.Finance.Providers.Core.Shared.ServerTime;

public interface IServerTimeSource
{
    long ServerTime { get; }
}
