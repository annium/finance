using System.Text.Json;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Connectors.Sync;

namespace Annium.Finance.Providers.Shared;

public static class ProviderRegistrationContextExtensions
{
    public static ProviderRegistrationContext AddHttpRequestFactoryWithJsonSerializer(
        this ProviderRegistrationContext ctx,
        string key,
        JsonSerializerOptions contracts
    )
    {
        ctx.Container.AddHttpRequestFactory(key);
        ctx.AddJsonSerializer(key, contracts);

        return ctx;
    }

    public static ProviderRegistrationContext AddJsonSerializer(
        this ProviderRegistrationContext ctx,
        string key,
        JsonSerializerOptions contracts
    )
    {
        ctx.Container.AddSerializers(key).WithJson(contracts);

        return ctx;
    }

    public static ProviderRegistrationContext WithUserSynchronizer<T>(this ProviderRegistrationContext ctx)
        where T : IUserSynchronizer
    {
        ctx.Container.Add<IUserSynchronizer, T>().Scoped();

        return ctx;
    }
}
