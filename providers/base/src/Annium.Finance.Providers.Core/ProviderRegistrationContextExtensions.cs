using System.Text.Json;
using Annium.Net.Http;
using Annium.Serialization.Abstractions;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Core;

/// <summary>
/// Extension methods for wiring HTTP request factories and JSON serialization into a
/// <see cref="ProviderRegistrationContext"/>.
/// </summary>
public static class ProviderRegistrationContextExtensions
{
    /// <summary>
    /// Registers a keyed HTTP request factory together with a matching keyed JSON serializer for
    /// <paramref name="key"/>.
    /// </summary>
    /// <param name="ctx">The registration context to add to.</param>
    /// <param name="key">The key the request factory and serializer are registered under.</param>
    /// <param name="contracts">The JSON serializer options to register.</param>
    /// <returns>The context, for chaining further registrations.</returns>
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

    /// <summary>
    /// Registers a keyed JSON serializer using the given options.
    /// </summary>
    /// <param name="ctx">The registration context to add to.</param>
    /// <param name="key">The key the serializer is registered under.</param>
    /// <param name="contracts">The JSON serializer options to register.</param>
    /// <returns>The context, for chaining further registrations.</returns>
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
