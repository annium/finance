using System.Text.Json;
using Annium.Net.Http;
using Annium.Serialization.Abstractions;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Core;

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
}
