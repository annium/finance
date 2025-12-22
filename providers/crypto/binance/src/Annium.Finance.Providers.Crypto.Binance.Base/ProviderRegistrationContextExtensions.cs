using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Crypto.Binance.Base.Internal.Market.Services;
using Annium.Finance.Providers.Crypto.Binance.Base.Market.Services;

namespace Annium.Finance.Providers.Crypto.Binance.Base;

public static class ProviderRegistrationContextExtensions
{
    public static ProviderRegistrationContext AddBookTickerServiceFactory(this ProviderRegistrationContext ctx)
    {
        ctx.Container.Add<IBookTickerServiceFactory, BookTickerServiceFactory>().Scoped();

        return ctx;
    }
}
