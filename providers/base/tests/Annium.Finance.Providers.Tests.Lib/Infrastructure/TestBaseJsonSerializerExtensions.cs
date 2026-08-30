using System;
using System.Net.Mime;
using Annium.Serialization.Abstractions;
using Annium.Serialization.Json;
using Annium.Testing;

namespace Annium.Finance.Providers.Tests.Lib.Infrastructure;

/// <summary>
/// Wires a JSON serializer into a <see cref="TestBase"/>-derived test so it can serialize or deserialize
/// payloads the same way a provider connector does.
/// </summary>
public static class TestBaseJsonSerializerExtensions
{
    /// <summary>
    /// Registers a JSON serializer a test can later retrieve with <see cref="GetJsonSerializer"/>.
    /// </summary>
    /// <param name="test">The test instance to register the serializer into.</param>
    /// <param name="key">The key the serializer is registered under.</param>
    public static void RegisterJsonSerializer(this TestBase test, string key = "")
    {
        test.Register(container =>
        {
            container.AddSerializers(key).WithJson(true);
        });
    }

    /// <summary>
    /// Resolves the JSON serializer previously registered with <see cref="RegisterJsonSerializer"/>.
    /// </summary>
    /// <param name="test">The test instance the serializer is registered on.</param>
    /// <param name="key">The key the serializer is registered under.</param>
    /// <returns>The registered JSON serializer.</returns>
    public static ISerializer<ReadOnlyMemory<byte>> GetJsonSerializer(this TestBase test, string key)
    {
        var serializerKey = SerializerKey.Create(key, MediaTypeNames.Application.Json);
        var serializer = test.GetKeyed<ISerializer<ReadOnlyMemory<byte>>>(serializerKey);

        return serializer;
    }
}
