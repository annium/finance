using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Models;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests;

public static class Users
{
    public static UserSettings Test { get; } =
        new(
            Constants.Provider,
            ProviderEnvironment.Test,
            "Qi8Vpp91GBBUcRc4RWWv6j28pq2cRlZFacOmtEwpaKcsas80aOrW7wPWP5Ba5WAs",
            "6pCRKWRNxpoTGsxv6GXbtvz2B6N2wnZAv1rp1stV3ZxgE7ibfK1rS8AGJT7JFA4D"
        );
}
