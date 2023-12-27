using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Models;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests;

public static class Users
{
    public static UserSettings Test { get; } =
        new(
            Constants.Provider,
            ProviderEnvironment.Test,
            "19136244bcbe0adb854f5234451ddf80c440ca7372fde16cb06178900712e8ba",
            "493495031de246dd8cfbcb3a3676df563c99abaf1240105af34567d440c1406e"
        );
}
