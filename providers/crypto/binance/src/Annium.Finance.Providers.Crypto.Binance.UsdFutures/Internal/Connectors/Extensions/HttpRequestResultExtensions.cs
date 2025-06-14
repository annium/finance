using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using Annium.Finance.Providers.Crypto.Binance.Base.Connectors.Extensions;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.Shared.Domain;
using Annium.Finance.Providers.Shared.Connectors.Extensions;
using Annium.Net.Http;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Connectors.Extensions;

public static class HttpRequestResultExtensions
{
    public static Task<MarketResult<T?>> AsMarketResultAsync<T>(this IHttpRequest request)
        where T : class
    {
        return request.AsMarketResultAsync<T, OperationResult>(
            HttpRequestHelper.GetFailureAsync,
            GetMarketErrorStatus,
            GetError
        );
    }

    public static Task<UserResult<T?>> AsUserResultAsync<T>(this IHttpRequest request)
        where T : class
    {
        return request.AsUserResultAsync<T, OperationResult>(
            HttpRequestHelper.GetFailureAsync,
            GetUserErrorStatus,
            GetError
        );
    }

    private static MarketOperationStatus? GetMarketErrorStatus(OperationResult result) =>
        result.Code switch
        {
            1 => MarketOperationStatus.NetworkError,
            2 => MarketOperationStatus.ParseError,
            3 => MarketOperationStatus.UnknownError,
            _ => null,
        };

    private static UserOperationStatus? GetUserErrorStatus(OperationResult result) =>
        result.Code switch
        {
            1 => UserOperationStatus.NetworkError,
            2 => UserOperationStatus.ParseError,
            3 => UserOperationStatus.UnknownError,
            -2018 => UserOperationStatus.InsufficientBalance, // BALANCE_NOT_SUFFICIENT
            -2019 => UserOperationStatus.InsufficientBalance, // MARGIN_NOT_SUFFICIENT
            _ => null,
        };

    private static string GetError(OperationResult result) => result.Message;
}
