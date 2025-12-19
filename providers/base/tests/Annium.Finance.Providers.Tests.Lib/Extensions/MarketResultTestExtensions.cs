using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Market.Operations;
using Annium.Testing;

namespace Annium.Finance.Providers.Tests.Lib.Extensions;

public static class MarketResultTestExtensions
{
    public static async Task<T> UnwrapAsync<T>(this Task<MarketResult<T>> task)
        where T : class
    {
#pragma warning disable VSTHRD003
        var result = await task;
#pragma warning restore VSTHRD003

        result.Message.Is(string.Empty);
        result.Status.Is(MarketOperationStatus.Ok);

        var data = result.Data;
        data.IsNotDefault();

        return data;
    }

    public static async Task UnwrapAsync(this Task<MarketResult> task)
    {
#pragma warning disable VSTHRD003
        var result = await task;
#pragma warning restore VSTHRD003

        result.Message.Is(string.Empty);
        result.Status.Is(MarketOperationStatus.Ok);
    }

    public static T Unwrap<T>(this MarketResult<T> result)
        where T : class
    {
        result.Message.Is(string.Empty);
        result.Status.Is(MarketOperationStatus.Ok);

        var data = result.Data;
        data.IsNotDefault();

        return data;
    }

    public static void Unwrap(this MarketResult result)
    {
        result.Message.Is(string.Empty);
        result.Status.Is(MarketOperationStatus.Ok);
    }

    public static async Task EnsureFailedAsync<T>(this Task<MarketResult<T>> task)
    {
#pragma warning disable VSTHRD003
        var result = await task;
#pragma warning restore VSTHRD003

        result.Message.IsNot(string.Empty);
        result.Status.IsNot(MarketOperationStatus.Ok);
    }

    public static async Task EnsureFailedAsync(this Task<MarketResult> task)
    {
#pragma warning disable VSTHRD003
        var result = await task;
#pragma warning restore VSTHRD003

        result.Message.IsNot(string.Empty);
        result.Status.IsNot(MarketOperationStatus.Ok);
    }

    public static void EnsureFailed<T>(this MarketResult<T> result)
    {
        result.Message.IsNot(string.Empty);
        result.Status.IsNot(MarketOperationStatus.Ok);
    }

    public static void EnsureFailed(this MarketResult result)
    {
        result.Message.IsNot(string.Empty);
        result.Status.IsNot(MarketOperationStatus.Ok);
    }
}
