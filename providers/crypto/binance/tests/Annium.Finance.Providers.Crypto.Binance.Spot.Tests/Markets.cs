using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Models;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests;

public static class Markets
{
    public static MarketSettings Real { get; } =
        new() { Provider = Constants.Provider, Environment = ProviderEnvironment.Real };
    public static MarketSettings Test { get; } =
        new() { Provider = Constants.Provider, Environment = ProviderEnvironment.Test };
}
