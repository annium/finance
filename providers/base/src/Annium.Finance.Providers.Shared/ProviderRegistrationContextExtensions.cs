using System.Text.Json;
using Annium.Core.DependencyInjection;

namespace Annium.Finance.Providers.Shared;

public static class ProviderRegistrationContextExtensions
{
    public static ProviderRegistrationContext AddHttpRequestFactoryWithJsonSerializer(
        this ProviderRegistrationContext ctx,
        string key,
        JsonSerializerOptions contracts
    )
    {
        ctx.Container.AddSerializers(key).WithJson(contracts);
        ctx.Container.AddHttpRequestFactory(key);

        return ctx;
    }
}
