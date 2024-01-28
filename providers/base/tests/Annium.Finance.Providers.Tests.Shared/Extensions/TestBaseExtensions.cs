using System;
using System.Net.Mime;
using Annium.Serialization.Abstractions;
using Annium.Testing;

namespace Annium.Finance.Providers.Tests.Shared.Extensions;

public static class TestBaseExtensions
{
    public static ISerializer<ReadOnlyMemory<byte>> GetJsonSerializer(this TestBase testBase, string key)
    {
        var serializerKey = SerializerKey.Create(key, MediaTypeNames.Application.Json);
        var serializer = testBase.GetKeyed<ISerializer<ReadOnlyMemory<byte>>>(serializerKey);

        return serializer;
    }

    public static void Inject<T>(this TestBase testBase, T value)
        where T : class
    {
        testBase.Get<Injected<T>>().Init(value);
    }
}
