using System;
using System.Net.Mime;
using Annium.Serialization.Abstractions;
using Annium.Testing;

namespace Annium.Finance.Providers.Tests.Lib.Extensions;

public static class TestBaseJsonSerializerExtensions
{
    public static ISerializer<ReadOnlyMemory<byte>> GetJsonSerializer(this TestBase testBase, string key)
    {
        var serializerKey = SerializerKey.Create(key, MediaTypeNames.Application.Json);
        var serializer = testBase.GetKeyed<ISerializer<ReadOnlyMemory<byte>>>(serializerKey);

        return serializer;
    }
}
