using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Tests.Shared.Connectors;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Connectors;

internal sealed record ProviderMarketEnvironments()
    : ProviderEnvironmentsBase(Constants.Provider, ProviderEnvironment.Real);

internal sealed record ProviderUserEnvironments()
    : ProviderEnvironmentsBase(Constants.Provider, ProviderEnvironment.Test);
