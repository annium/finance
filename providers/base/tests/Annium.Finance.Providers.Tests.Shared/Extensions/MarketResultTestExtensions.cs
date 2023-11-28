using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using Annium.Testing;

namespace Annium.Finance.Providers.Tests.Shared.Extensions;

public static class MarketResultTestExtensions
{
    public static async Task<T> Unwrap<T>(this Task<MarketResult<T>> task)
        where T : class
    {
        var result = await task;

        result.Message.IsNullOrWhiteSpace().IsTrue();
        result.Status.Is(MarketOperationStatus.Ok);

        var data = result.Data;
        data.IsNotDefault();

        return data;
    }

    public static async Task Unwrap(this Task<MarketResult> task)
    {
        var result = await task;

        result.Message.IsNullOrWhiteSpace().IsTrue();
        result.Status.Is(MarketOperationStatus.Ok);
    }

    public static T Unwrap<T>(this MarketResult<T> result)
        where T : class
    {
        result.Message.IsNullOrWhiteSpace().IsTrue();
        result.Status.Is(MarketOperationStatus.Ok);

        var data = result.Data;
        data.IsNotDefault();

        return data;
    }

    public static void Unwrap(this MarketResult result)
    {
        result.Message.IsNullOrWhiteSpace().IsTrue();
        result.Status.Is(MarketOperationStatus.Ok);
    }

    public static async Task EnsureFailed<T>(this Task<MarketResult<T>> task)
    {
        var result = await task;

        result.Message.IsNullOrWhiteSpace().IsFalse();
        result.Status.IsNot(MarketOperationStatus.Ok);
    }

    public static async Task EnsureFailed(this Task<MarketResult> task)
    {
        var result = await task;

        result.Message.IsNullOrWhiteSpace().IsFalse();
        result.Status.IsNot(MarketOperationStatus.Ok);
    }

    public static void EnsureFailed<T>(this MarketResult<T> result)
    {
        result.Message.IsNullOrWhiteSpace().IsFalse();
        result.Status.IsNot(MarketOperationStatus.Ok);
    }

    public static void EnsureFailed(this MarketResult result)
    {
        result.Message.IsNullOrWhiteSpace().IsFalse();
        result.Status.IsNot(MarketOperationStatus.Ok);
    }
}
