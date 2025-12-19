using System.Net;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.User.Operations;
using Annium.Finance.Providers.Core.User.HttpExtensions;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Domain;
using Annium.Net.Http;
using OneOf;
using static Annium.Finance.Providers.Crypto.Binance.Base.Shared.HttpExtensions.HttpRequestHelper;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.HttpExtensions;

internal static class HttpRequestUserResultExtensions
{
    public static Task<UserResult<T?>> AsUserResultAsync<T>(this IHttpRequest request)
        where T : class
    {
        return request.AsUserResultAsync<T, OperationResult>(GetFailureAsync, MapResponse);
    }

    private static UserResult<T?> MapResponse<T>(IHttpResponse<OneOf<T, OperationResult>> response)
    {
        // if response mapped to success
        if (response.Data.IsT0)
        {
            var data = response.Data.AsT0;

            // if response is successful - return Ok
            if (response.IsSuccess)
                return UserResult.Ok<T?>(data);

            // otherwise response is mapped to success, but is failure - use response.StatusCode
            {
                var status = MapStatusCode(response.StatusCode);
                return UserResult.New<T?>(status, data);
            }
        }

        // if response mapped to error, OperationResult - use it to construct response
        {
            var error = response.Data.AsT1;
            var status = MapOperationCode(error.Code);

            return UserResult.New<T?>(status, default, error.Message);
        }
    }

    private static UserOperationStatus MapStatusCode(HttpStatusCode code) =>
        code switch
        {
            (HttpStatusCode)418 or HttpStatusCode.TooManyRequests => UserOperationStatus.TooManyRequests,
            HttpStatusCode.BadRequest => UserOperationStatus.BadRequest,
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => UserOperationStatus.Forbidden,
            HttpStatusCode.NotFound => UserOperationStatus.NotFound,
            _ => UserOperationStatus.UnknownError,
        };

    private static UserOperationStatus MapOperationCode(long code) =>
        code switch
        {
            OperationResult.NetworkError => UserOperationStatus.NetworkError,
            OperationResult.Aborted => UserOperationStatus.Aborted,
            OperationResult.ParseError => UserOperationStatus.ParseError,
            -2018 => UserOperationStatus.InsufficientBalance, // BALANCE_NOT_SUFFICIENT
            -2019 => UserOperationStatus.InsufficientBalance, // MARGIN_NOT_SUFFICIENT
            < 0 => UserOperationStatus.BadRequest,
            _ => UserOperationStatus.UnknownError,
        };
}
