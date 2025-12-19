namespace Annium.Finance.Providers.Core.Shared.TimeSync;

public interface IServerTimeSource
{
    long ServerTime { get; }
}
