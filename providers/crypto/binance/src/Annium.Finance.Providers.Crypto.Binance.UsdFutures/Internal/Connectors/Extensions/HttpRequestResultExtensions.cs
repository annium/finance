using System;
using System.Net.Http;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using Annium.Finance.Providers.Crypto.Binance.Base.Connectors.Extensions;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.Shared.Domain;
using Annium.Finance.Providers.Shared.Connectors.Extensions;
using Annium.Net.Http;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Connectors.Extensions;

public static class HttpRequestResultExtensions
{
    private static async Task<OperationResult> GetFailure(
        HttpFailureReason reason,
        HttpContent content,
        Exception? e
    ) =>
        reason switch
        {
            HttpFailureReason.Abort => new OperationResult(1, "Request aborted"),
            HttpFailureReason.Parse
                => new OperationResult(1, $"Response parse failed. Content: {await content.ReadAsStringAsync()}"),
            HttpFailureReason.Exception
                => new OperationResult(
                    1,
                    $"Request failed. Error: {e?.Message}. Content: {await content.ReadAsStringAsync()}"
                ),
            _ => new OperationResult(1, "Unmapped failure")
        };

    public static Task<MarketResult<T?>> AsMarketResultAsync<T>(this IHttpRequest request)
        where T : class
    {
        return request.AsMarketResultAsync<T, OperationResult>(HttpRequestHelper.GetFailure, GetError);
    }

    public static Task<UserResult<T?>> AsUserResultAsync<T>(this IHttpRequest request)
        where T : class
    {
        return request.AsUserResultAsync<T, OperationResult>(HttpRequestHelper.GetFailure, GetErrorStatus, GetError);
    }

    private static UserOperationStatus? GetErrorStatus(OperationResult result) =>
        result.Code switch
        {
            -2018 => UserOperationStatus.InsufficientBalance, // BALANCE_NOT_SUFFICIENT
            -2019 => UserOperationStatus.InsufficientBalance, // MARGIN_NOT_SUFFICIENT
            _ => null
        };

    private static string GetError(OperationResult result) => result.Message;
}
