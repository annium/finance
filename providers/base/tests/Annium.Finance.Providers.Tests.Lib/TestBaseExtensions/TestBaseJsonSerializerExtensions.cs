using System;
using System.Net.Mime;
using Annium.Serialization.Abstractions;
using Annium.Serialization.Json;
using Annium.Testing;

namespace Annium.Finance.Providers.Tests.Lib.TestBaseExtensions;

public static class TestBaseJsonSerializerExtensions
{
    public static void RegisterJsonSerializer(this TestBase test, string key = "")
    {
        test.Register(container =>
        {
            container.AddSerializers(key).WithJson(true);
        });
    }

    public static ISerializer<ReadOnlyMemory<byte>> GetJsonSerializer(this TestBase test, string key)
    {
        var serializerKey = SerializerKey.Create(key, MediaTypeNames.Application.Json);
        var serializer = test.GetKeyed<ISerializer<ReadOnlyMemory<byte>>>(serializerKey);

        return serializer;
    }
}
