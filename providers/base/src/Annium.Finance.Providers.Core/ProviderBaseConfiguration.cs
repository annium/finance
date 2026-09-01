using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Finance.Providers.Core.Shared.TimeSync;

namespace Annium.Finance.Providers.Core;

/// <summary>
/// Registration-time configuration for a single finance provider, passed to
/// <see cref="ProviderRegistrationContext.AddProvider{TMarketProviderFactory, TMarketConnectorFactory, TUserProviderFactory, TUserConnectorFactory, TFinanceService}"/>.
/// </summary>
/// <param name="Provider">The provider's key, used to keep its keyed DI registrations apart from other providers'.</param>
/// <param name="ServerTime">The timing configuration for this provider's server time source.</param>
public sealed record ProviderBaseConfiguration(string Provider, ServerTimeProviderConfig ServerTime);
